using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed record TradeSettlementResult(
    Trade? Trade,
    Guid? RejectedOrderId,
    int BuyOrderFilledQuantity,
    int SellOrderFilledQuantity)
{
    public bool IsExecuted => Trade is not null;

    public static TradeSettlementResult Executed(Trade trade, int buyFilled, int sellFilled) =>
        new(trade, null, buyFilled, sellFilled);

    public static TradeSettlementResult Rejected(Guid orderId) => new(null, orderId, 0, 0);
}
