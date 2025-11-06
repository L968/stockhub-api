using System.Collections.Concurrent;
using Stockhub.Consumers.MatchingEngine.Application.Queues;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class DirtyQueueTests
{
    private readonly DirtyQueue _queue;

    public DirtyQueueTests()
    {
        _queue = new DirtyQueue();
    }

    [Fact]
    public void ConcurrentDequeue_ShouldBeThreadSafe()
    {
        // Arrange
        var stockIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        var dequeuedIds = new ConcurrentBag<Guid>();

        foreach (Guid id in stockIds)
        {
            _queue.Enqueue(id);
        }

        // Act
        Parallel.For(0, stockIds.Count, _ =>
        {
            if (_queue.TryDequeue(out Guid dequeuedId))
            {
                dequeuedIds.Add(dequeuedId);
            }
        });

        // Assert
        Assert.Equal(stockIds.Count, dequeuedIds.Count);
        Assert.All(dequeuedIds, id => Assert.Contains(id, stockIds));
    }

    [Fact]
    public void ConcurrentEnqueue_ShouldBeThreadSafe()
    {
        // Arrange
        var stockIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        var results = new ConcurrentDictionary<Guid, bool>();

        // Act
        Parallel.ForEach(stockIds, id =>
        {
            bool enqueued = _queue.Enqueue(id);
            results[id] = enqueued;
        });

        // Assert
        foreach (Guid id in stockIds)
        {
            Assert.True(results[id]);
            Assert.True(_queue.IsProcessing(id));
        }
    }

    [Fact]
    public void ConcurrentMarkProcessed_ShouldBeThreadSafe()
    {
        // Arrange
        var stockIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();

        foreach (Guid id in stockIds)
        {
            _queue.Enqueue(id);
            _queue.TryDequeue(out _);
        }

        // Act
        Parallel.ForEach(stockIds, id => _queue.MarkProcessed(id));

        // Assert
        foreach (Guid id in stockIds)
        {
            Assert.False(_queue.IsProcessing(id));
            Assert.False(_queue.IsDirty(id));
        }
    }

    [Fact]
    public void Enqueue_ShouldAddNewStock_ReturnTrue()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act
        bool result = _queue.Enqueue(stockId);

        // Assert
        Assert.True(result);
        Assert.True(_queue.IsProcessing(stockId));
        Assert.True(_queue.IsDirty(stockId));
    }

    [Fact]
    public void Enqueue_ShouldNotAddDuplicateStock_ReturnFalse()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);

        // Act
        bool result = _queue.Enqueue(stockId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDirty_ShouldReturnTrue_WhenStockIsProcessing()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);

        // Act & Assert
        Assert.True(_queue.IsDirty(stockId));

        _queue.TryDequeue(out _);
        Assert.True(_queue.IsDirty(stockId));

        _queue.MarkProcessed(stockId);
        Assert.False(_queue.IsDirty(stockId));
    }

    [Fact]
    public void IsProcessing_ShouldReturnTrue_OnlyWhenInProcessingDictionary()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act & Assert
        Assert.False(_queue.IsProcessing(stockId));

        _queue.Enqueue(stockId);
        Assert.True(_queue.IsProcessing(stockId));

        _queue.TryDequeue(out _);
        Assert.True(_queue.IsProcessing(stockId));

        _queue.MarkProcessed(stockId);
        Assert.False(_queue.IsProcessing(stockId));
    }

    [Fact]
    public void MarkProcessed_ShouldDoNothing_WhenStockNotProcessing()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act
        _queue.MarkProcessed(stockId);

        // Assert
        Assert.False(_queue.IsProcessing(stockId));
    }

    [Fact]
    public void MarkProcessed_ShouldRemoveStockFromProcessing()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);
        _queue.TryDequeue(out _);

        // Act
        _queue.MarkProcessed(stockId);

        // Assert
        Assert.False(_queue.IsProcessing(stockId));
        Assert.False(_queue.IsDirty(stockId));
    }

    [Fact]
    public void MixedOperations_Concurrently_ShouldNotThrowExceptions()
    {
        // Arrange
        var stockIds = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var exceptions = new ConcurrentQueue<Exception>();

        // Act
        Parallel.Invoke(
            () =>
            {
                try
                {
                    foreach (Guid id in stockIds.Take(250))
                    {
                        _queue.Enqueue(id);
                    }
                }
                catch (Exception ex) { exceptions.Enqueue(ex); }
            },
            () =>
            {
                try
                {
                    foreach (Guid id in stockIds.Skip(250))
                    {
                        _queue.Enqueue(id);
                    }
                }
                catch (Exception ex) { exceptions.Enqueue(ex); }
            },
            () =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        _queue.TryDequeue(out _);
                    }
                }
                catch (Exception ex) { exceptions.Enqueue(ex); }
            },
            () =>
            {
                try
                {
                    foreach (Guid id in stockIds.Take(100))
                    {
                        _queue.MarkProcessed(id);
                    }
                }
                catch (Exception ex) { exceptions.Enqueue(ex); }
            }
        );

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void Scenario_CompleteWorkflow()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act & Assert
        Assert.True(_queue.Enqueue(stockId));
        Assert.True(_queue.IsProcessing(stockId));
        Assert.True(_queue.IsDirty(stockId));

        Assert.True(_queue.TryDequeue(out Guid dequeuedId));
        Assert.Equal(stockId, dequeuedId);
        Assert.True(_queue.IsProcessing(stockId));

        _queue.MarkProcessed(stockId);
        Assert.False(_queue.IsProcessing(stockId));
        Assert.False(_queue.IsDirty(stockId));

        Assert.True(_queue.Enqueue(stockId));
        Assert.True(_queue.IsProcessing(stockId));
    }

    [Fact]
    public void TryDequeue_ShouldReturnFalse_WhenQueueEmpty()
    {
        // Arrange

        // Act
        bool result = _queue.TryDequeue(out Guid dequeuedStockId);

        // Assert
        Assert.False(result);
        Assert.Equal(Guid.Empty, dequeuedStockId);
    }

    [Fact]
    public void TryDequeue_ShouldReturnTrueAndRemoveFromQueue_WhenItemExists()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        _queue.Enqueue(stockId);

        // Act
        bool result = _queue.TryDequeue(out Guid dequeuedStockId);

        // Assert
        Assert.True(result);
        Assert.Equal(stockId, dequeuedStockId);
        Assert.True(_queue.IsProcessing(stockId));
        Assert.True(_queue.IsDirty(stockId));
    }

    [Fact]
    public void TryDequeue_ShouldSkipItemsThatWereMarkedProcessed()
    {
        // Arrange
        var stockId1 = Guid.NewGuid();
        var stockId2 = Guid.NewGuid();

        _queue.Enqueue(stockId1);
        _queue.Enqueue(stockId2);
        _queue.MarkProcessed(stockId1);

        // Act
        bool result = _queue.TryDequeue(out Guid dequeuedStockId);

        // Assert
        Assert.True(result);
        Assert.Equal(stockId2, dequeuedStockId);
        Assert.False(_queue.IsProcessing(stockId1));
        Assert.True(_queue.IsProcessing(stockId2));
    }
}
