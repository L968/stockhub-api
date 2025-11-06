namespace Stockhub.Consumers.MatchingEngine.Application.Queues;

public interface IDirtyQueue
{
    bool Enqueue(Guid stockId);
    bool TryDequeue(out Guid stockId);
    void MarkProcessed(Guid stockId);
    bool IsProcessing(Guid stockId);
}
