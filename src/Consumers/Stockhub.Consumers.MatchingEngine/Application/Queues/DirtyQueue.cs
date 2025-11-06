using System.Collections.Concurrent;

namespace Stockhub.Consumers.MatchingEngine.Application.Queues;

internal sealed class DirtyQueue : IDirtyQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly ConcurrentDictionary<Guid, byte> _pendingStocks = new();

    public bool Enqueue(Guid stockId)
    {
        if (_pendingStocks.TryAdd(stockId, 0))
        {
            _queue.Enqueue(stockId);
            return true;
        }
        return false;
    }

    public bool TryDequeue(out Guid stockId)
    {
        while (_queue.TryDequeue(out stockId))
        {
            if (_pendingStocks.ContainsKey(stockId))
            {
                return true;
            }
        }

        stockId = Guid.Empty;
        return false;
    }

    public void MarkProcessed(Guid stockId)
    {
        _pendingStocks.TryRemove(stockId, out _);
    }

    public bool IsProcessing(Guid stockId)
    {
        return _pendingStocks.ContainsKey(stockId);
    }

    public bool IsDirty(Guid stockId)
    {
        return IsProcessing(stockId);
    }
}
