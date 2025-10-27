﻿using Microsoft.Extensions.Logging;
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
    public void CreateTrade_Should_Create_Valid_Trade()
    {
        // Arrange
        var buyer = new User { Id = Guid.NewGuid() };
        var seller = new User { Id = Guid.NewGuid() };
        var proposal = new TradeProposal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50, 10);

        // Act
        var trade = typeof(MatchingEngineService)
            .GetMethod("CreateTrade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [proposal, buyer, seller]) as Trade;

        // Assert
        Assert.NotNull(trade);
        Assert.Equal(proposal.StockId, trade.StockId);
        Assert.Equal(10, trade.Quantity);
        Assert.Equal(50, trade.Price);
    }

    [Fact]
    public async Task ProcessAsync_Should_Add_Order_And_Remove_OrderBook_When_Empty()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order order = CreateOrder(stockId: stockId, side: OrderSide.Sell);
        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([]);

        // Act
        List<Trade> trades = await _service.ProcessAsync(order, CancellationToken.None);

        // Assert
        Assert.Empty(trades);
    }

    [Fact]
    public async Task ProcessAsync_Should_Cancel_Buyer_Without_Balance_And_Continue_With_Next_Orders()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        Order buy1 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Buy, price: 100, quantity: 5);
        Order buy2 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Buy, price: 101, quantity: 5);
        Order buy3 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Buy, price: 102, quantity: 5);
        Order buy4 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Buy, price: 103, quantity: 5);
        Order sell1 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 10);

        var user1 = new User { Id = buy1.UserId, CurrentBalance = 1000 };
        var user2 = new User { Id = buy2.UserId, CurrentBalance = 1000 };
        var user3 = new User { Id = buy3.UserId, CurrentBalance = 0 };
        var user4 = new User { Id = buy4.UserId, CurrentBalance = 1000 };
        var user5 = new User { Id = sell1.UserId, CurrentBalance = 0 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buy1, buy2, buy3, buy4, sell1]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([user1, user2, user3, user4, user5]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(sell1, CancellationToken.None);

        // Assert
        Assert.True(buy3.IsCancelled);

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
    public async Task ProcessAsync_Should_Cancel_BuyOrder_If_Buyer_Has_Insufficient_Balance_And_Not_Execute_Any_Trade()
    {
        // Arrange
        var stockId = Guid.NewGuid();

        Order buyIncoming = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Buy, price: 100, quantity: 10);

        Order sell1 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 3);
        Order sell2 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 4);
        Order sell3 = CreateOrder(Guid.NewGuid(), stockId, OrderSide.Sell, price: 100, quantity: 5);

        var buyer = new User { Id = buyIncoming.UserId, CurrentBalance = 500 };
        var seller1 = new User { Id = sell1.UserId, CurrentBalance = 0 };
        var seller2 = new User { Id = sell2.UserId, CurrentBalance = 0 };
        var seller3 = new User { Id = sell3.UserId, CurrentBalance = 0 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyIncoming, sell1, sell2, sell3]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller1, seller2, seller3]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyIncoming, CancellationToken.None);

        // Assert
        Assert.True(buyIncoming.IsCancelled);
        Assert.Empty(executedTrades);

        Assert.Equal(500, buyer.CurrentBalance);
        Assert.Equal(0, buyIncoming.FilledQuantity);
        Assert.Equal(0, sell1.FilledQuantity);
        Assert.Equal(0, sell2.FilledQuantity);
        Assert.Equal(0, sell3.FilledQuantity);

        _loggerMock.VerifyLog(LogLevel.Warning, "Insufficient balance", Times.AtLeast(1));
    }


    [Fact]
    public async Task ProcessAsync_Should_Cancel_BuyOrder_When_Buyer_Has_Insufficient_Balance_And_Incoming_Is_Sell()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 50 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(sellOrder, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Warning, "Insufficient balance", Times.AtLeast(1));
        Assert.True(buyOrder.IsCancelled);
        Assert.False(sellOrder.IsCancelled);
        Assert.Empty(executedTrades);
        Assert.Equal(50, buyer.CurrentBalance);
        Assert.Equal(500, seller.CurrentBalance);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(0, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task ProcessAsync_Should_Cancel_Order_When_Buyer_Has_Insufficient_Balance()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 50 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Warning, "Insufficient balance", Times.AtLeast(1));
        Assert.True(buyOrder.IsCancelled);
        Assert.Empty(executedTrades);
        Assert.Equal(50, buyer.CurrentBalance);
        Assert.Equal(500, seller.CurrentBalance);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(0, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task ProcessAsync_Should_Execute_Trade_When_Buyer_Has_Sufficient_Balance()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        var tradesList = new List<Trade>();
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet(tradesList);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);
        _usersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder, CancellationToken.None);

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
    public async Task ProcessAsync_Should_Handle_Cancelled_Orders_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        Order cancelledOrder = CreateOrder(stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10, isCancelled: true);

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([cancelledOrder]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        Exception exception = await Record.ExceptionAsync(() =>
            _service.ProcessAsync(cancelledOrder, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ProcessAsync_Should_Handle_Multiple_Stocks_Independently()
    {
        // Arrange
        var stockId1 = Guid.NewGuid();
        var stockId2 = Guid.NewGuid();

        var buyer1 = new User { Id = Guid.NewGuid(), CurrentBalance = 2000 };
        var seller1 = new User { Id = Guid.NewGuid(), CurrentBalance = 500 };

        Order buyOrder1 = CreateOrder(userId: buyer1.Id, stockId: stockId1, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder1 = CreateOrder(userId: seller1.Id, stockId: stockId1, side: OrderSide.Sell, price: 100, quantity: 10);

        Order buyOrder2 = CreateOrder(stockId: stockId2, side: OrderSide.Buy, price: 100, quantity: 10);

        var allOrders = new List<Order> { buyOrder1, sellOrder1, buyOrder2 };
        var allUsers = new List<User> { buyer1, seller1 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet(allOrders);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet(allUsers);
        _usersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder1, CancellationToken.None);

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
    public async Task ProcessAsync_Should_Handle_Partial_Fills_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 5);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);
        _usersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder, CancellationToken.None);

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
    public async Task ProcessAsync_Should_Log_Trade_Execution()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000 };
        var seller = new User { Id = sellerId, CurrentBalance = 500 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);
        _usersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "Trade executed", Times.AtLeast(1));
        Assert.Single(executedTrades);
    }

    [Fact]
    public async Task ProcessAsync_Should_Not_Match_Orders_With_Same_Side_Buy()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyer1Id = Guid.NewGuid();
        var buyer2Id = Guid.NewGuid();

        Order buyOrder1 = CreateOrder(userId: buyer1Id, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order buyOrder2 = CreateOrder(userId: buyer2Id, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);

        var user1 = new User { Id = buyer1Id, CurrentBalance = 1000 };
        var user2 = new User { Id = buyer2Id, CurrentBalance = 1000 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder1, buyOrder2]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([user1, user2]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder1, CancellationToken.None);

        // Assert
        Assert.Empty(executedTrades);
        _ordersDbContextMock.Verify(x => x.Trades.Add(It.IsAny<Trade>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_Should_Not_Match_Orders_With_Same_Side_Sell()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        Order sellOrder1 = CreateOrder(userId: seller1Id, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);
        Order sellOrder2 = CreateOrder(userId: seller2Id, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var user1 = new User { Id = seller1Id, CurrentBalance = 1000 };
        var user2 = new User { Id = seller2Id, CurrentBalance = 1000 };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([sellOrder1, sellOrder2]);
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet([]);
        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([user1, user2]);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(sellOrder1, CancellationToken.None);

        // Assert
        Assert.Empty(executedTrades);
        _ordersDbContextMock.Verify(x => x.Trades.Add(It.IsAny<Trade>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_Should_Update_User_Balances_Correctly()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        Order buyOrder = CreateOrder(userId: buyerId, stockId: stockId, side: OrderSide.Buy, price: 100, quantity: 10);
        Order sellOrder = CreateOrder(userId: sellerId, stockId: stockId, side: OrderSide.Sell, price: 100, quantity: 10);

        var buyer = new User { Id = buyerId, CurrentBalance = 2000m };
        var seller = new User { Id = sellerId, CurrentBalance = 500m };

        _ordersDbContextMock.Setup(x => x.Orders).ReturnsDbSet([buyOrder, sellOrder]);
        var tradesList = new List<Trade>();
        _ordersDbContextMock.Setup(x => x.Trades).ReturnsDbSet(tradesList);
        _ordersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _usersDbContextMock.Setup(x => x.Users).ReturnsDbSet([buyer, seller]);
        _usersDbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.StartAsync(CancellationToken.None);

        // Act
        List<Trade> executedTrades = await _service.ProcessAsync(buyOrder, CancellationToken.None);

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
