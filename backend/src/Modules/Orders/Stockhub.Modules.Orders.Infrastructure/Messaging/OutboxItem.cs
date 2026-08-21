namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed record OutboxItem(Guid Id, Guid StockId, string Payload);
