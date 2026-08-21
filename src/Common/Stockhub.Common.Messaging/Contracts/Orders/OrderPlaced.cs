namespace Stockhub.Common.Messaging.Contracts.Orders;

public sealed record OrderPlaced(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    Guid StockId,
    OrderSide Side,
    decimal Price,
    int Quantity,
    DateTime CreatedAtUtc,
    DateTime OccurredAtUtc
);

public enum OrderSide
{
    Buy = 0,
    Sell = 1
}
