using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderBookRepositoryTests
{
    private readonly OrderBookRepository _repository;

    public OrderBookRepositoryTests()
    {
        _repository = new OrderBookRepository();
    }

    private static Order CreateOrder(
        Guid? id = null,
        Guid? stockId = null,
        OrderSide side = OrderSide.Buy,
        decimal price = 100m,
        int quantity = 10)
    {
        return new Order
        {
            Id = id ?? Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StockId = stockId ?? Guid.NewGuid(),
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = 0,
            IsCancelled = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public void BuildFromOrders_ShouldClearExistingAndAddNewOrders()
    {
        // Arrange
        Order existingOrder = CreateOrder();
        _repository.AddOrder(existingOrder);
        var newOrders = new List<Order> { CreateOrder(), CreateOrder() };

        // Act
        _repository.BuildFromOrders(newOrders);

        // Assert
        foreach (Order order in newOrders)
        {
            OrderBook snapshot = _repository.GetOrderBookSnapshot(order.StockId);
            Assert.Equal(1, snapshot.Count);
        }
    }

    [Fact]
    public void AddOrder_ShouldAddOrderToRepository()
    {
        // Arrange
        Order order = CreateOrder();

        // Act
        _repository.AddOrder(order);

        // Assert
        OrderBook snapshot = _repository.GetOrderBookSnapshot(order.StockId);
        Assert.Equal(1, snapshot.Count);
    }

    [Fact]
    public void CancelOrder_ShouldMarkOrderAsCancelledAndRemoveIt()
    {
        // Arrange
        Order order = CreateOrder();
        _repository.AddOrder(order);

        // Act
        _repository.CancelOrder(order.Id);

        // Assert
        Assert.True(order.IsCancelled);
        OrderBook snapshot = _repository.GetOrderBookSnapshot(order.StockId);
        Assert.Equal(0, snapshot.Count);
    }

    [Fact]
    public void UpdateOrderFilledQuantity_ShouldUpdateFilledQuantity()
    {
        // Arrange
        Order order = CreateOrder(quantity: 10);
        _repository.AddOrder(order);

        // Act
        _repository.UpdateOrderFilledQuantity(order.Id, 5);

        // Assert
        Assert.Equal(5, order.FilledQuantity);
    }

    [Fact]
    public void RemoveOrder_ShouldRemoveOrderFromRepository()
    {
        // Arrange
        Order order = CreateOrder();
        _repository.AddOrder(order);

        // Act
        _repository.RemoveOrder(order.Id);

        // Assert
        OrderBook snapshot = _repository.GetOrderBookSnapshot(order.StockId);
        Assert.Equal(0, snapshot.Count);
    }

    [Fact]
    public void GetOrderBookSnapshot_ShouldReturnOrdersForSpecificStock()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order order1 = CreateOrder(stockId: stockId);
        Order order2 = CreateOrder(stockId: stockId);
        Order order3 = CreateOrder();
        _repository.AddOrder(order1);
        _repository.AddOrder(order2);
        _repository.AddOrder(order3);

        // Act
        OrderBook snapshot = _repository.GetOrderBookSnapshot(stockId);

        // Assert
        Assert.Equal(2, snapshot.Count);

        foreach (Order order in snapshot.Orders)
        {
            Assert.Equal(stockId, order.StockId);
        }
    }
}
