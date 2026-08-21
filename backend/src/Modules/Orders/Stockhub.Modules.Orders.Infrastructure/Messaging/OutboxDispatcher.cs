using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Stockhub.Modules.Orders.Infrastructure.Messaging;

internal sealed class OutboxDispatcher(
    IOutboxRepository repository,
    IOrderStreamPublisher publisher,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var lockId = Guid.CreateVersion7();
            IReadOnlyList<OutboxItem> items = await repository.ClaimAsync(lockId, stoppingToken);

            if (items.Count == 0)
            {
                await Task.Delay(100, stoppingToken);
                continue;
            }

            Guid[] ids = items.Select(item => item.Id).ToArray();

            try
            {
                await publisher.PublishAsync(items, stoppingToken);
                await repository.MarkPublishedAsync(lockId, ids, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Failed to publish {Count} outbox messages", items.Count);
                await repository.ReleaseAsync(lockId, ids, exception.Message, stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
