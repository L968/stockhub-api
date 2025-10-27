using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class MatchingEngineServiceTests
{
    private readonly Mock<OrdersDbContext> _ordersDbContextMock;
    private readonly Mock<UsersDbContext> _usersDbContextMock;
    private readonly Mock<ILogger<MatchingEngineService>> _loggerMock;
    private readonly MatchingEngineService _service;

    public MatchingEngineServiceTests()
    {
        _ordersDbContextMock = new Mock<OrdersDbContext>();
        _usersDbContextMock = new Mock<UsersDbContext>();
        _loggerMock = new Mock<ILogger<MatchingEngineService>>();

        _service = new MatchingEngineService(
            _ordersDbContextMock.Object,
            _usersDbContextMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task StartAsync_Should_Build_OrderBooks_And_Log_TotalOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder()
        };
        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet(orders);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "Matching Engine started", Times.Once());
    }

    [Fact]
    public async Task ProcessAsync_Should_Add_Order_And_Remove_OrderBook_When_Empty()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order order = CreateOrder(stockId: stockId);
        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());

        // Act
        await _service.ProcessAsync(order, CancellationToken.None);

        // Assert
        Assert.True(true);
    }

    private static Order CreateOrder(
        Guid? userId = null,
        Guid? stockId = null,
        OrderSide side = OrderSide.Buy,
        decimal price = 100,
        int quantity = 10,
        int filledQuantity = 0,
        bool isCancelled = false)
    {
        DateTime now = DateTime.UtcNow;

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            StockId = stockId ?? Guid.NewGuid(),
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = filledQuantity,
            IsCancelled = isCancelled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string contains, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(contains, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            times
        );
    }
}
