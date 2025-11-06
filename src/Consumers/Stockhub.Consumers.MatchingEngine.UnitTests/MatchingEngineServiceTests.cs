using Microsoft.Extensions.Logging;
using Moq;
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
    private readonly Mock<IDirtyQueue> _dirtyQueueMock;
    private readonly Mock<ILogger<MatchingEngineService>> _loggerMock;
    private readonly MatchingEngineService _service;

    public MatchingEngineServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _dirtyQueueMock = new Mock<IDirtyQueue>();
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
            new OrderBookRepository(),
            _orderRepositoryMock.Object,
            _userRepositoryMock.Object,
            _dirtyQueueMock.Object,
            new OrderValidator(_userRepositoryMock.Object),
            _loggerMock.Object
        );
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

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Return_Empty_When_OrderBook_Has_No_Trades()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        // Act
        List<Trade> trades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Empty(trades);
        _dirtyQueueMock.Verify(x => x.MarkProcessed(stockId), Times.Once);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Cancel_Buyer_Without_Balance_And_Continue_With_Next_Orders()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        Order buy1 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 100, quantity: 5);
        Order buy2 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 101, quantity: 5);
        Order buy3 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 102, quantity: 5);
        Order buy4 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 103, quantity: 5);
        Order sell1 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 10);

        var user1 = new User { Id = buy1.UserId, CurrentBalance = 1000 };
        var user2 = new User { Id = buy2.UserId, CurrentBalance = 1000 };
        var user3 = new User { Id = buy3.UserId, CurrentBalance = 0 };
        var user4 = new User { Id = buy4.UserId, CurrentBalance = 1000 };
        var user5 = new User { Id = sell1.UserId, CurrentBalance = 0 };

        Order[] allOrders = [buy1, buy2, buy3, buy4, sell1];
        User[] allUsers = [user1, user2, user3, user4, user5];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, decimal amount, CancellationToken _) =>
            {
                User user = allUsers.First(u => u.Id == userId);
                return user.CurrentBalance >= amount;
            });

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> trades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(x => x.CancelAsync(buy3.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.DoesNotContain(trades, t => t.BuyOrderId == buy3.Id);
        Assert.Equal(2, trades.Count);

        Assert.Collection(trades,
            t =>
            {
                Assert.Equal(stockId, t.StockId);
                Assert.Equal(buy4.UserId, t.BuyerId);
                Assert.Equal(sell1.UserId, t.SellerId);
                Assert.Equal(100, t.Price);
                Assert.Equal(5, t.Quantity);
            },
            t =>
            {
                Assert.Equal(stockId, t.StockId);
                Assert.Equal(buy2.UserId, t.BuyerId);
                Assert.Equal(sell1.UserId, t.SellerId);
                Assert.Equal(100, t.Price);
                Assert.Equal(5, t.Quantity);
            });
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Cancel_BuyOrder_If_Buyer_Has_Insufficient_Balance_And_Not_Execute_Any_Trade()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        Order buyIncoming = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 100, quantity: 10);
        Order sell1 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 3);
        Order sell2 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 4);
        Order sell3 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 5);

        var buyer = new User { Id = buyIncoming.UserId, CurrentBalance = 500 };
        var seller1 = new User { Id = sell1.UserId, CurrentBalance = 0 };
        var seller2 = new User { Id = sell2.UserId, CurrentBalance = 0 };
        var seller3 = new User { Id = sell3.UserId, CurrentBalance = 0 };

        Order[] allOrders = [sell1, sell2, sell3];
        User[] allUsers = [buyer, seller1, seller2, seller3];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.Concat([buyIncoming]).First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, decimal amount, CancellationToken _) =>
            {
                User user = allUsers.First(u => u.Id == userId);
                return user.CurrentBalance >= amount;
            });

        await _service.InitializeAsync(CancellationToken.None);
        await _service.EnqueueOrderAsync(buyIncoming, CancellationToken.None);

        // Act
        List<Trade> trades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(x => x.CancelAsync(buyIncoming.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(trades);
        Assert.Equal(500, buyer.CurrentBalance);
        Assert.Equal(0, buyIncoming.FilledQuantity);
        Assert.Equal(0, sell1.FilledQuantity);
        Assert.Equal(0, sell2.FilledQuantity);
        Assert.Equal(0, sell3.FilledQuantity);
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid order", Times.AtLeast(1));
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Cancel_BuyOrder_When_Buyer_Has_Insufficient_Balance_And_Incoming_Is_Sell()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 50 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, decimal amount, CancellationToken _) =>
                allUsers.First(u => u.Id == userId).CurrentBalance >= amount);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid order", Times.AtLeast(1));
        _orderRepositoryMock.Verify(x => x.CancelAsync(buyOrder.Id, It.IsAny<CancellationToken>()), Times.Once);

        Assert.False(sellOrder.IsCancelled);
        Assert.Empty(executedTrades);
        Assert.Equal(50, buyer.CurrentBalance);
        Assert.Equal(500, seller.CurrentBalance);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(0, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Cancel_Order_When_Buyer_Has_Insufficient_Balance()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 50 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, decimal amount, CancellationToken _) =>
                allUsers.First(u => u.Id == userId).CurrentBalance >= amount);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid order", Times.AtLeast(1));
        _orderRepositoryMock.Verify(x => x.CancelAsync(buyOrder.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(executedTrades);
        Assert.Equal(50, buyer.CurrentBalance);
        Assert.Equal(500, seller.CurrentBalance);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(0, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Execute_Trade_When_Buyer_Has_Sufficient_Balance()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Single(executedTrades);
        Trade trade = executedTrades[0];

        Assert.Equal(stockId, trade.StockId);
        Assert.Equal(buyerId, trade.BuyerId);
        Assert.Equal(sellerId, trade.SellerId);
        Assert.Equal(buyOrder.Id, trade.BuyOrderId);
        Assert.Equal(sellOrder.Id, trade.SellOrderId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(10, trade.Quantity);

        Assert.Equal(1000, buyer.CurrentBalance);
        Assert.Equal(1500, seller.CurrentBalance);
        Assert.Equal(10, buyOrder.FilledQuantity);
        Assert.Equal(10, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Handle_Cancelled_Orders_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order cancelledOrder = CreateOrder(stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10, isCancelled: true);

        Order[] allOrders = [cancelledOrder];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelledOrder);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        Exception exception = await Record.ExceptionAsync(() =>
            _service.MatchPendingOrdersAsync(stockId, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Handle_Multiple_Stocks_Independently()
    {
        // Arrange
        var stockId1 = Guid.NewGuid();
        var stockId2 = Guid.NewGuid();

        var buyer1 = new User { Id = Guid.NewGuid(), CurrentBalance = 2000 };
        var seller1 = new User { Id = Guid.NewGuid(), CurrentBalance = 500 };

        Order buyOrder1 = CreateOrder(userId: buyer1.Id, stockId: stockId1, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder1 = CreateOrder(userId: seller1.Id, stockId: stockId1, side: OrderSide.Sell, price: 100, quantity: 10);
        Order buyOrder2 = CreateOrder(stockId: stockId2, side: OrderSide.Buy, price: 100, quantity: 10);

        Order[] allOrders = [buyOrder1, sellOrder1, buyOrder2];
        User[] allUsers = [buyer1, seller1];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.FirstOrDefault(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId1, CancellationToken.None);

        // Assert
        Assert.Single(executedTrades);
        Trade trade = executedTrades[0];

        Assert.Equal(stockId1, trade.StockId);
        Assert.Equal(buyer1.Id, trade.BuyerId);
        Assert.Equal(seller1.Id, trade.SellerId);
        Assert.Equal(buyOrder1.Id, trade.BuyOrderId);
        Assert.Equal(sellOrder1.Id, trade.SellOrderId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(10, trade.Quantity);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Handle_Partial_Fills_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 5);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Single(executedTrades);
        Trade trade = executedTrades[0];
        Assert.Equal(stockId, trade.StockId);
        Assert.Equal(buyerId, trade.BuyerId);
        Assert.Equal(sellerId, trade.SellerId);
        Assert.Equal(buyOrder.Id, trade.BuyOrderId);
        Assert.Equal(sellOrder.Id, trade.SellOrderId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(5, trade.Quantity);

        Assert.Equal(1500, buyer.CurrentBalance);
        Assert.Equal(1000, seller.CurrentBalance);
        Assert.Equal(5, buyOrder.FilledQuantity);
        Assert.Equal(5, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Log_Trade_Execution()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "Trade executed", Times.AtLeast(1));
        Assert.Single(executedTrades);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Not_Execute_Trade_If_Order_Is_Cancelled_In_Repository()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        Order buy1 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 100, quantity: 5);
        Order buy2 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 101, quantity: 5);
        Order buy3 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 102, quantity: 5);
        Order buy4 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Buy, price: 103, quantity: 5);
        Order sell1 = CreateOrder(Guid.NewGuid(), Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 10);

        var user1 = new User { Id = buy1.UserId, CurrentBalance = 1000 };
        var user2 = new User { Id = buy2.UserId, CurrentBalance = 1000 };
        var user3 = new User { Id = buy3.UserId, CurrentBalance = 0 };
        var user4 = new User { Id = buy4.UserId, CurrentBalance = 1000 };
        var user5 = new User { Id = sell1.UserId, CurrentBalance = 0 };

        Order[] allOrders = [buy1, buy2, buy3, buy4, sell1];
        User[] allUsers = [user1, user2, user3, user4, user5];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, decimal amount, CancellationToken _) =>
            {
                User user = allUsers.First(u => u.Id == userId);
                return user.CurrentBalance >= amount;
            });

        await _service.InitializeAsync(CancellationToken.None);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(buy3.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateOrder(buy3.Id, buy3.UserId, buy3.StockId, buy3.Side, buy3.Price, buy3.Quantity, buy3.FilledQuantity, true));

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        _orderRepositoryMock.Verify(x => x.CancelAsync(buy3.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.DoesNotContain(executedTrades, t => t.BuyOrderId == buy3.Id);
        Assert.Equal(2, executedTrades.Count);

        Assert.Collection(executedTrades,
            t =>
            {
                Assert.Equal(stockId, t.StockId);
                Assert.Equal(buy4.UserId, t.BuyerId);
                Assert.Equal(sell1.UserId, t.SellerId);
                Assert.Equal(buy4.Id, t.BuyOrderId);
                Assert.Equal(sell1.Id, t.SellOrderId);
                Assert.Equal(100, t.Price);
                Assert.Equal(5, t.Quantity);
            },
            t =>
            {
                Assert.Equal(stockId, t.StockId);
                Assert.Equal(buy2.UserId, t.BuyerId);
                Assert.Equal(sell1.UserId, t.SellerId);
                Assert.Equal(buy2.Id, t.BuyOrderId);
                Assert.Equal(sell1.Id, t.SellOrderId);
                Assert.Equal(100, t.Price);
                Assert.Equal(5, t.Quantity);
            }
        );
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Not_Match_Orders_With_Same_Side_Buy()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyer1Id = Guid.NewGuid();
        var buyer2Id = Guid.NewGuid();

        Order buyOrder1 = CreateOrder(userId: buyer1Id, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order buyOrder2 = CreateOrder(userId: buyer2Id, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);

        var user1 = new User { Id = buyer1Id, CurrentBalance = 1000 };
        var user2 = new User { Id = buyer2Id, CurrentBalance = 1000 };

        Order[] allOrders = [buyOrder1, buyOrder2];
        User[] allUsers = [user1, user2];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Empty(executedTrades);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Not_Match_Orders_With_Same_Side_Sell()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        Order sellOrder1 = CreateOrder(userId: seller1Id, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);
        Order sellOrder2 = CreateOrder(userId: seller2Id, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var user1 = new User { Id = seller1Id, CurrentBalance = 1000 };
        var user2 = new User { Id = seller2Id, CurrentBalance = 1000 };

        Order[] allOrders = [sellOrder1, sellOrder2];
        User[] allUsers = [user1, user2];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Empty(executedTrades);
    }

    [Fact]
    public async Task MatchPendingOrdersAsync_Should_Update_User_Balances_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000m };
        var seller = new User { Id = sellerId, CurrentBalance = 500m };

        Order[] allOrders = [buyOrder, sellOrder];
        User[] allUsers = [buyer, seller];

        _orderRepositoryMock
            .Setup(x => x.GetAllOpenOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allOrders);

        _orderRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                Order original = allOrders.First(o => o.Id == id);
                return CloneOrder(original);
            });

        _userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => allUsers.First(u => u.Id == id));

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.MatchPendingOrdersAsync(stockId, CancellationToken.None);

        // Assert
        Assert.Single(executedTrades);
        Assert.Equal(1000m, buyer.CurrentBalance);
        Assert.Equal(1500m, seller.CurrentBalance);
        Trade trade = executedTrades[0];
        Assert.Equal(buyOrder.Id, trade.BuyOrderId);
        Assert.Equal(sellOrder.Id, trade.SellOrderId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(10, trade.Quantity);
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

    private static Order CloneOrder(Order source)
    {
        return new Order
        {
            Id = source.Id,
            UserId = source.UserId,
            StockId = source.StockId,
            Side = source.Side,
            Price = source.Price,
            Quantity = source.Quantity,
            FilledQuantity = source.FilledQuantity,
            IsCancelled = source.IsCancelled,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
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
