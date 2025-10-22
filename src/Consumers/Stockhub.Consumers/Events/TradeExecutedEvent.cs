namespace Stockhub.Consumers.Events;

public sealed record TradeExecutedEvent(
    Guid TradeId,
    Guid UserId,
    string Symbol,
    decimal Price,
    int Quantity,
    DateTime ExecutedAtUtc
);
