using Microsoft.EntityFrameworkCore;
using Stockhub.Common.Application.DomainEvent;
using Stockhub.Common.Domain.DomainEvents;
using Stockhub.Common.Infrastructure.Outbox;
using Stockhub.Modules.Stocks.Infrastructure.Database;

namespace Stockhub.Modules.Stocks.Infrastructure.Outbox;

internal sealed class IdempotentDomainEventHandler<TDomainEvent>(
    IDomainEventHandler<TDomainEvent> decorated,
    StocksDbContext dbContext)
    : DomainEventHandler<TDomainEvent> where TDomainEvent : IDomainEvent
{
    public override async Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var outboxMessageConsumer = new OutboxMessageConsumer(domainEvent.Id, decorated.GetType().Name);

        if (await OutboxConsumerExistsAsync(outboxMessageConsumer))
        {
            return;
        }

        await decorated.Handle(domainEvent, cancellationToken);

        dbContext.OutboxMessageConsumers.Add(outboxMessageConsumer);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> OutboxConsumerExistsAsync(OutboxMessageConsumer outboxMessageConsumer)
    {
        return await dbContext.OutboxMessageConsumers
            .AsNoTracking()
            .AnyAsync(omc => omc.OutboxMessageId == outboxMessageConsumer.OutboxMessageId && omc.Name == outboxMessageConsumer.Name);
    }
}
