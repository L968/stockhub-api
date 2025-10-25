using System.Numerics;
using Stockhub.Consumers.Entities;
using Stockhub.Consumers.Events.Debezium;

namespace Stockhub.Consumers.Events.OrderPlaced;

internal sealed class OrderPlacedMapper
{
    public OrderPlacedEvent Map(DebeziumPayload<OrderEventPayload> payload)
    {
        OrderEventPayload after = payload.After!;

        byte[] priceBytes = Convert.FromBase64String(after.Price);
        BigInteger unscaledPrice = new(priceBytes.Reverse().ToArray());
        decimal price = (decimal)unscaledPrice / 100m;

        return new OrderPlacedEvent
        {
            OrderId = after.Id,
            UserId = after.User_Id,
            StockId = after.Stock_Id,
            Side = (OrderSide)after.Side,
            Price = price,
            Quantity = after.Quantity,
            FilledQuantity = after.Filled_Quantity,
            Status = (OrderStatus)after.Status,
            CreatedAtUtc = after.Created_At,
            UpdatedAtUtc = after.Updated_At
        };
    }
}
