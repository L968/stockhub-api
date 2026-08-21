using Stockhub.Common.Messaging.Contracts.Orders;

namespace Stockhub.Modules.Orders.Application.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken);
}
