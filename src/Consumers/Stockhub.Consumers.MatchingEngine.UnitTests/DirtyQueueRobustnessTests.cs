using System.Collections.Concurrent;
using Stockhub.Consumers.MatchingEngine.Application.Queues;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class DirtyQueueRobustnessTests
{
    private readonly DirtyQueue _queue;

    public DirtyQueueRobustnessTests()
    {
        _queue = new DirtyQueue();
    }

    [Fact]
    public void TryDequeue_ShouldNotReturnProcessedItems()
    {
        // Arrange
        var stockId1 = Guid.NewGuid();
        var stockId2 = Guid.NewGuid();
        var stockId3 = Guid.NewGuid();

        _queue.Enqueue(stockId1);
        _queue.Enqueue(stockId2);
        _queue.Enqueue(stockId3);
        _queue.MarkProcessed(stockId2);

        // Act
        var dequeuedIds = new List<Guid>();
        while (_queue.TryDequeue(out Guid id))
        {
            dequeuedIds.Add(id);
        }

        // Assert
        Assert.Contains(stockId1, dequeuedIds);
        Assert.Contains(stockId3, dequeuedIds);
        Assert.DoesNotContain(stockId2, dequeuedIds);
        Assert.Equal(2, dequeuedIds.Count);
    }

    [Fact]
    public void MultipleMarkProcessed_ShouldBeIdempotent()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);
        _queue.TryDequeue(out _);

        // Act
        _queue.MarkProcessed(stockId);
        _queue.MarkProcessed(stockId);
        _queue.MarkProcessed(stockId);

        // Assert
        Assert.False(_queue.IsProcessing(stockId));
    }

    [Fact]
    public void Enqueue_After_MarkProcessed_ShouldWork()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);
        _queue.TryDequeue(out _);
        _queue.MarkProcessed(stockId);

        // Act
        bool result = _queue.Enqueue(stockId);

        // Assert
        Assert.True(result);
        Assert.True(_queue.IsProcessing(stockId));
    }

    [Fact]
    public void TryDequeue_EmptyQueue_After_Processing()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);
        _queue.TryDequeue(out _);
        _queue.MarkProcessed(stockId);

        // Act
        bool result = _queue.TryDequeue(out Guid dequeuedId);

        // Assert
        Assert.False(result);
        Assert.Equal(Guid.Empty, dequeuedId);
    }

    [Fact]
    public void HighConcurrency_ShouldMaintainConsistency()
    {
        // Arrange
        var stockIds = Enumerable.Range(0, 10000).Select(_ => Guid.NewGuid()).ToList();
        var processed = new ConcurrentBag<Guid>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.ForEach(stockIds, id =>
        {
            try
            {
                if (_queue.Enqueue(id))
                {
                    if (!_queue.TryDequeue(out Guid dequeuedId))
                    {
                        return;
                    }

                    _queue.MarkProcessed(dequeuedId);
                    processed.Add(dequeuedId);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(stockIds.Count, processed.Count);
        Assert.All(processed, id => Assert.False(_queue.IsProcessing(id)));
    }
}
