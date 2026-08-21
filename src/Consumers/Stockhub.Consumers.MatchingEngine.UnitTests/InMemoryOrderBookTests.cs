using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public sealed class InMemoryOrderBookTests
{
    [Fact]
    public void RemovePartition_RemovesOnlyOwnedStocks()
    {
        var repository = new OrderBookRepository();
        Order first = CreateOrder(Guid.NewGuid());
        Order second = CreateOrder(Guid.NewGuid());

        repository.AddOrder("orders-0", first);
        repository.AddOrder("orders-1", second);
        repository.RemovePartition("orders-0");

        Assert.False(repository.ContainsOrder(first.Id));
        Assert.True(repository.ContainsOrder(second.Id));
    }

    [Fact]
    public void ReplacePartition_RebuildsItsLocalBooks()
    {
        var repository = new OrderBookRepository();
        Order oldOrder = CreateOrder(Guid.NewGuid());
        Order currentOrder = CreateOrder(Guid.NewGuid());

        repository.AddOrder("orders-0", oldOrder);
        repository.ReplacePartition("orders-0", [currentOrder]);

        Assert.False(repository.ContainsOrder(oldOrder.Id));
        Assert.True(repository.ContainsOrder(currentOrder.Id));
    }

    private static Order CreateOrder(Guid stockId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        StockId = stockId,
        Side = OrderSide.Buy,
        Price = 100,
        Quantity = 10,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
