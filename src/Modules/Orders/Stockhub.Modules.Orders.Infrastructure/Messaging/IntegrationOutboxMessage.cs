using System.Text.Json;
using Stockhub.Common.Messaging.Contracts.Orders;

namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed class IntegrationOutboxMessage
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid StockId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public Guid? LockId { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public string? LastError { get; private set; }

    private IntegrationOutboxMessage()
    {
    }

    public static IntegrationOutboxMessage Create(OrderPlaced message) => new()
    {
        Id = message.EventId,
        OrderId = message.OrderId,
        StockId = message.StockId,
        Type = nameof(OrderPlaced),
        Payload = JsonSerializer.Serialize(message),
        OccurredAtUtc = message.OccurredAtUtc
    };
}
