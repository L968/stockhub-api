using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stockhub.Consumers.MatchingEngine.Application.Queues;
using Stockhub.Consumers.MatchingEngine.Application.Services;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Workers;

internal sealed class MatchingWorkerHostedService(
    IDirtyQueue dirtyQueue,
    IMatchingEngineService matchingEngineService,
    ILogger<MatchingWorkerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!dirtyQueue.TryDequeue(out Guid stockId))
            {
                await Task.Delay(5, stoppingToken);
                continue;
            }

            try
            {
                await matchingEngineService.MatchPendingOrdersAsync(stockId, stoppingToken);
                logger.LogInformation("Processing stock {StockId}", stockId);
            }
            finally
            {
                dirtyQueue.MarkProcessed(stockId);
            }
        }
    }
}
