using Microsoft.Extensions.Logging;
using Moq;
using Stockhub.Consumers.MatchingEngine.Application.Cache;
using Stockhub.Consumers.MatchingEngine.Application.Queues;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class MatchingEngineServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITradeExecutor> _tradeExecutorMock;
    private readonly Mock<IDirtyQueue> _dirtyQueueMock;
    private readonly Mock<IProcessedOrderCache> _processedOrderCacheMock;
    private readonly Mock<ILogger<MatchingEngineService>> _loggerMock;
    private readonly OrderBookRepository _orderBookRepository;
    private readonly MatchingEngineService _service;

    public MatchingEngineServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _tradeExecutorMock = new Mock<ITradeExecutor>();
        _dirtyQueueMock = new Mock<IDirtyQueue>();
        _processedOrderCacheMock = new Mock<IProcessedOrderCache>();
        _orderBookRepository = new OrderBookRepository();
        _loggerMock = new Mock<ILogger<MatchingEngineService>>();

        _orderRepositoryMock
            .Setup(x => x.UpdateFilledQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.AddTradeAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.UpdateBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new MatchingEngineService(
            _orderBookRepository,
            _orderRepositoryMock.Object,
            _tradeExecutorMock.Object,
            _dirtyQueueMock.Object,
            new OrderValidator(_userRepositoryMock.Object),
            _processedOrderCacheMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Add_Order_And_Enqueue_Stock_When_Valid()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 100, quantity: 5);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        Assert.True(_orderBookRepository.ContainsOrder(order.Id));
        _dirtyQueueMock.Verify(x => x.Enqueue(stockId), Times.Once);
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Cancel_Order_When_Validation_Fails()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 0);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(x => x.CancelAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueOrderAsync_Should_Not_Add_Duplicate_Order()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Order order = CreateOrder(userId: userId, stockId: stockId, price: 100, quantity: 5);

        _userRepositoryMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, CurrentBalance = 1000 });

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(userId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderBookRepository.AddOrder(order);

        // Act
        await _service.EnqueueOrderAsync(order, CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(It.IsAny<Guid>()), Times.Never);
        Assert.Equal(1, _orderBookRepository.GetOrderBookSnapshot(stockId).Count);
    }

    [Fact]
    public async Task InitializeAsync_Should_Build_OrderBook_And_Enqueue_Distinct_Stocks()
    {
        // Arrange
        var stockA = Guid.NewGuid();
        var stockB = Guid.NewGuid();

        Order[] openOrders =
        [
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockB)
        ];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(openOrders);

        // Act
        await _service.InitializeAsync(CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(stockA), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(stockB), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_Should_Build_OrderBooks_And_Enqueue_All_Stocks()
    {
        // Arrange
        var stockA = Guid.NewGuid();
        var stockB = Guid.NewGuid();
        Order[] orders =
        [
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockA),
            CreateOrder(stockId: stockB)
        ];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        await _service.InitializeAsync(CancellationToken.None);

        // Assert
        _dirtyQueueMock.Verify(x => x.Enqueue(stockA), Times.Once);
        _dirtyQueueMock.Verify(x => x.Enqueue(stockB), Times.Once);
    }

    private static Order CreateOrder(
        Guid? orderId = null,
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
            Id = orderId ?? Guid.CreateVersion7(),
            UserId = userId ?? Guid.CreateVersion7(),
            StockId = stockId ?? Guid.CreateVersion7(),
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
