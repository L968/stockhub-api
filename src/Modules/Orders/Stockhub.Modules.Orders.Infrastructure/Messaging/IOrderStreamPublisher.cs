namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal interface IOrderStreamPublisher
{
    Task PublishAsync(IReadOnlyCollection<OutboxItem> items, CancellationToken cancellationToken);
}
