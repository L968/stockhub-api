using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class TradeExecutor(
    ITradeSettlementRepository settlementRepository,
    IOrderBookRepository orderBookRepository,
    ILogger<TradeExecutor> logger) : ITradeExecutor
{
    public async Task<Result<Trade>> ExecuteAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        TradeSettlementResult settlement = await settlementRepository.ExecuteAsync(proposal, cancellationToken);

        if (!settlement.IsExecuted)
        {
            orderBookRepository.CancelOrder(settlement.RejectedOrderId!.Value);
            return Result.Failure<Trade>(
                Error.Conflict("Trade.Rejected", "The order can no longer be settled."));
        }

        Trade trade = settlement.Trade!;
        orderBookRepository.UpdateOrderFilledQuantity(proposal.BuyOrderId, settlement.BuyOrderFilledQuantity);
        orderBookRepository.UpdateOrderFilledQuantity(proposal.SellOrderId, settlement.SellOrderFilledQuantity);

        logger.LogInformation(
            "Trade executed: {StockId} | Buy {BuyOrderId} ↔ Sell {SellOrderId} @ {Price} x {Quantity}",
            trade.StockId,
            trade.BuyOrderId,
            trade.SellOrderId,
            trade.Price,
            trade.Quantity);

        return Result.Success(trade);
    }
}
