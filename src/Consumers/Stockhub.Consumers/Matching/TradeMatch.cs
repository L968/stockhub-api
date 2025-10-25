namespace Stockhub.Consumers.Matching;

internal sealed record TradeMatch(
    Guid BuyOrderId,
    Guid SellOrderId,
    decimal Price,
    int Quantity
);
