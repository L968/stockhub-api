using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Cache;
using Stockhub.Consumers.MatchingEngine.Application.Queues;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    IOrderBookRepository orderBookRepository,
    IOrderRepository orderRepository,
    ITradeExecutor tradeExecutor,
    IDirtyQueue dirtyQueue,
    OrderValidator orderValidator,
    IProcessedOrderCache processedOrderCache,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Order> openOrders = await orderRepository.GetAllOpenOrdersAsync(cancellationToken);

        orderBookRepository.BuildFromOrders(openOrders);

        foreach (Guid stockId in openOrders.Select(o => o.StockId).Distinct())
        {
            dirtyQueue.Enqueue(stockId);
        }

        logger.LogInformation("Matching Engine started with {Count} existing orders", openOrders.Count());
    }

    public async Task EnqueueOrderAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        if (processedOrderCache.Exists(incomingOrder.Id))
        {
            logger.LogDebug("Order {OrderId} ignored (already processed)", incomingOrder.Id);
            return;
        }

        Result validation = await ValidateOrderAsync(incomingOrder, cancellationToken);
        if (validation.IsFailure)
        {
            await CommitCancelOrder(incomingOrder, cancellationToken);
            return;
        }

        if (orderBookRepository.ContainsOrder(incomingOrder.Id))
        {
            return;
        }

        orderBookRepository.AddOrder(incomingOrder);
        dirtyQueue.Enqueue(incomingOrder.StockId);
    }

    public async Task<List<Trade>> MatchPendingOrdersAsync(Guid stockId, CancellationToken cancellationToken)
    {
        OrderBook orderBook = orderBookRepository.GetOrderBookSnapshot(stockId);

        if (orderBook.Count == 0)
        {
            dirtyQueue.MarkProcessed(stockId);
            return [];
        }

        var executedTrades = new List<Trade>();
        int safetyLimit = orderBook.Count * 2;
        int iterationCount = 0;

        while (iterationCount++ < safetyLimit)
        {
            List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

            if (!proposals.Any())
            {
                dirtyQueue.MarkProcessed(stockId);
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
                $"Potential infinite loop detected while matching stock {stockId}. Iteration limit ({safetyLimit}) exceeded."
            );
        }

        return executedTrades;
    }

    private async Task<Result> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
    {
        ValidationResult validation = await orderValidator.ValidateAsync(order, cancellationToken);

        if (validation.IsValid)
        {
            return Result.Success();
        }

        var validationError = new ValidationError(
            validation.Errors.Select(f => Error.Problem(f.ErrorCode ?? f.PropertyName, f.ErrorMessage)).ToArray()
        );

        logger.LogWarning(
            "Invalid order {OrderId} (User {UserId}, Stock {StockId}): {Errors}",
            order.Id, order.UserId, order.StockId,
            string.Join("; ", validationError.Errors.Select(e => e.Description))
        );

        return Result.Failure(validationError);
    }

    private async Task CommitCancelOrder(Order order, CancellationToken cancellationToken)
    {
        await orderRepository.CancelAsync(order.Id, cancellationToken);
        orderBookRepository.CancelOrder(order.Id);
    }
}
