using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    IOrderBookRepository orderBookRepository,
    ITradeExecutor tradeExecutor,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task<IReadOnlyList<Trade>> ProcessOrderAsync(
        string partition,
        Order incomingOrder,
        CancellationToken cancellationToken)
    {
        if (orderBookRepository.ContainsOrder(incomingOrder.Id))
        {
            logger.LogDebug("Order {OrderId} ignored because it is already in memory", incomingOrder.Id);
            return [];
        }

        orderBookRepository.AddOrder(partition, incomingOrder);
        return await MatchPendingOrdersAsync(incomingOrder.StockId, cancellationToken);
    }

    private async Task<IReadOnlyList<Trade>> MatchPendingOrdersAsync(
        Guid stockId,
        CancellationToken cancellationToken)
    {
        OrderBook orderBook = orderBookRepository.GetOrderBookSnapshot(stockId);

        if (orderBook.Count == 0)
        {
            return [];
        }

        var executedTrades = new List<Trade>();
        int safetyLimit = orderBook.Count * 2;
        int iterationCount = 0;

        while (iterationCount++ < safetyLimit)
        {
            List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

            if (proposals.Count == 0)
            {
                break;
            }

            foreach (TradeProposal proposal in proposals)
            {
                Result<Trade> result = await tradeExecutor.ExecuteAsync(proposal, cancellationToken);

                if (result.IsFailure)
                {
                    break;
                }

                executedTrades.Add(result.Value);
            }
        }

        if (iterationCount >= safetyLimit)
        {
            throw new InvalidOperationException(
                $"Potential infinite loop detected while matching stock {stockId}. Iteration limit ({safetyLimit}) exceeded.");
        }

        return executedTrades;
    }
}
