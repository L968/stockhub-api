namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxItem>> ClaimAsync(Guid lockId, CancellationToken cancellationToken);
    Task MarkPublishedAsync(Guid lockId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid lockId, IReadOnlyCollection<Guid> ids, string error, CancellationToken cancellationToken);
}
