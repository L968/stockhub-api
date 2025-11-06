using System.Collections.Concurrent;

namespace Stockhub.Consumers.MatchingEngine.Application.Queues;

internal sealed class DirtyQueue : IDirtyQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly ConcurrentDictionary<Guid, byte> _processingStocks = new();

    public bool Enqueue(Guid stockId)
    {
        if (_processingStocks.TryAdd(stockId, 0))
        {
            _queue.Enqueue(stockId);
            return true;
        }
        return false;
    }

    public bool TryDequeue(out Guid stockId)
    {
        if (_queue.TryDequeue(out stockId))
        {
            return true;
        }

        stockId = Guid.Empty;
        return false;
    }

    public void MarkProcessed(Guid stockId) => _processingStocks.TryRemove(stockId, out _);

    public bool IsProcessing(Guid stockId) => _processingStocks.ContainsKey(stockId);

    public bool IsDirty(Guid stockId) => _queue.Contains(stockId) || IsProcessing(stockId);
}

