using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, Task> _runningTasks = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!dirtyQueue.TryDequeue(out Guid stockId))
            {
                await Task.Delay(100, stoppingToken);
                continue;
            }

            if (_runningTasks.TryAdd(stockId, Task.CompletedTask))
            {
                Task task = ProcessStockAsync(stockId, stoppingToken);
                _runningTasks[stockId] = task;
            }
        }
    }

    private async Task ProcessStockAsync(Guid stockId, CancellationToken stoppingToken)
    {
        try
        {
            await matchingEngineService.MatchPendingOrdersAsync(stockId, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing stock {StockId}", stockId);
        }
        finally
        {
            _runningTasks.TryRemove(stockId, out _);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_runningTasks.Values);
        await base.StopAsync(cancellationToken);
    }
}
