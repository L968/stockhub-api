namespace Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

internal sealed record TradeProposal(
    Guid StockId,
    Guid BuyOrderId,
    Guid SellOrderId,
    decimal Price,
    int Quantity
);
