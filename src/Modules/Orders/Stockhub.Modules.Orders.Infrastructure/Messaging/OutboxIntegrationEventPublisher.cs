using Stockhub.Common.Messaging.Contracts.Orders;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Infrastructure.Database;

namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed class OutboxIntegrationEventPublisher(OrdersDbContext dbContext)
    : IIntegrationEventPublisher
{
    public Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        dbContext.IntegrationOutbox.Add(IntegrationOutboxMessage.Create(message));
        return Task.CompletedTask;
    }
}
