using System.Numerics;
using Stockhub.Common.Messaging.Consumers.Debezium;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Events;
using Stockhub.Consumers.MatchingEngine.Domain.Events.OrderPlaced;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka.Mappers;

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
