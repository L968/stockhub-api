using Microsoft.Extensions.Logging;
using Moq;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class TradeExecutorTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IOrderBookRepository> _orderBookRepositoryMock = new();
    private readonly Mock<ILogger<TradeExecutor>> _loggerMock = new();
    private readonly OrderValidator _orderValidator;
    private readonly TradeExecutor _tradeExecutor;

    public TradeExecutorTests()
    {
        _orderValidator = new OrderValidator(_userRepositoryMock.Object);

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tradeExecutor = new TradeExecutor(
            _orderRepositoryMock.Object,
            _userRepositoryMock.Object,
            _orderBookRepositoryMock.Object,
            _orderValidator,
            _loggerMock.Object
        );
    }

    private static Order CreateOrder(
        Guid? userId = null,
        Guid? stockId = null,
        int quantity = 10,
        decimal price = 100,
        OrderSide side = OrderSide.Buy,
        int filledQuantity = 0,
        bool isCancelled = false,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null)
    {
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
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow
        };
    }

    private static User CreateUser(
        Guid? id = null,
        string? email = null,
        string? fullName = null,
        decimal balance = 1000,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null)
    {
        Guid userId = id ?? Guid.NewGuid();

        return new User
        {
            Id = userId,
            Email = email ?? $"user_{userId}@example.com",
            FullName = fullName ?? $"User {userId}",
            CurrentBalance = balance,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow
        };
    }

    private static TradeProposal CreateTradeProposal(
        Guid stockId,
        Guid? buyOrderId = null,
        Guid? sellOrderId = null,
        decimal price = 100,
        int quantity = 10) =>
        new(stockId, buyOrderId ?? Guid.NewGuid(), sellOrderId ?? Guid.NewGuid(), price, quantity);

    [Fact]
    public async Task ExecuteAsync_Should_AccumulateFilledQuantities_AfterMultipleExecutions()
    {
        // Arrange
        Order buyOrder = CreateOrder(quantity: 20);
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId, quantity: 20);
        User buyer = CreateUser(id: buyOrder.UserId, balance: 5000);
        User seller = CreateUser(id: sellOrder.UserId, balance: 0);
        TradeProposal proposal1 = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);
        TradeProposal proposal2 = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(buyer);
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        // Act
        Result<Trade> firstResult = await _tradeExecutor.ExecuteAsync(proposal1, CancellationToken.None);
        Result<Trade> secondResult = await _tradeExecutor.ExecuteAsync(proposal2, CancellationToken.None);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(20, buyOrder.FilledQuantity);
        Assert.Equal(20, sellOrder.FilledQuantity);
        _orderRepositoryMock.Verify(x => x.UpdateFilledQuantityAsync(buyOrder.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _orderRepositoryMock.Verify(x => x.AddTradeAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_Should_CancelBuyOrder_WhenBuyOrderInvalid()
    {
        // Arrange
        Order buyOrder = CreateOrder(quantity: 0);
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId, quantity: 10);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        _orderRepositoryMock.Verify(x => x.CancelAsync(buyOrder.Id, It.IsAny<CancellationToken>()), Times.Once);
        _orderBookRepositoryMock.Verify(x => x.CancelOrder(buyOrder.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CancelSellOrder_WhenSellOrderInvalid()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        Order sellOrder = CreateOrder(quantity: 0, userId: Guid.NewGuid(), stockId: buyOrder.StockId);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        _orderRepositoryMock.Verify(x => x.CancelAsync(sellOrder.Id, It.IsAny<CancellationToken>()), Times.Once);
        _orderBookRepositoryMock.Verify(x => x.CancelOrder(sellOrder.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ExecuteTrade_When_OrdersValid()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId, quantity: 10);
        User buyer = CreateUser(id: buyOrder.UserId, balance: 1000);
        User seller = CreateUser(id: sellOrder.UserId, balance: 500);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(buyer);
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, buyOrder.FilledQuantity);
        Assert.Equal(10, sellOrder.FilledQuantity);
        Assert.Equal(0, buyer.CurrentBalance - 1000 + result.Value.TotalValue);
        Assert.Equal(0, seller.CurrentBalance - 500 - result.Value.TotalValue);

        _orderRepositoryMock.Verify(x => x.UpdateFilledQuantityAsync(buyOrder.Id, 10, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.UpdateFilledQuantityAsync(sellOrder.Id, 10, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.AddTradeAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderBookRepositoryMock.Verify(x => x.UpdateOrderFilledQuantity(buyOrder.Id, 10), Times.Once);
        _orderBookRepositoryMock.Verify(x => x.UpdateOrderFilledQuantity(sellOrder.Id, 10), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ExecuteTrade_WhenOrdersFullyFilled()
    {
        // Arrange
        Order buyOrder = CreateOrder(quantity: 10);
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId, quantity: 10);
        User buyer = CreateUser(id: buyOrder.UserId, balance: 2000);
        User seller = CreateUser(id: sellOrder.UserId, balance: 0);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(buyer);
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(buyOrder.Quantity, buyOrder.FilledQuantity);
        Assert.Equal(sellOrder.Quantity, sellOrder.FilledQuantity);
        _orderRepositoryMock.Verify(x => x.AddTradeAsync(It.IsAny<Trade>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.CancelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_WhenBuyerHasInsufficientBalance()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);

        _userRepositoryMock
            .Setup(x => x.HasSufficientBalanceAsync(buyOrder.UserId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        _orderRepositoryMock.Verify(x => x.CancelAsync(buyOrder.Id, It.IsAny<CancellationToken>()), Times.Once);
        _orderBookRepositoryMock.Verify(x => x.CancelOrder(buyOrder.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PartiallyFillOrders_WhenQuantityLessThanOrder()
    {
        // Arrange
        Order buyOrder = CreateOrder(quantity: 20);
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId, quantity: 20);
        User buyer = CreateUser(id: buyOrder.UserId, balance: 1000);
        User seller = CreateUser(id: sellOrder.UserId, balance: 500);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id, 100, 10);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(buyer);
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        // Act
        Result<Trade> result = await _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, buyOrder.FilledQuantity);
        Assert.Equal(10, sellOrder.FilledQuantity);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_WhenBuyerNotFound()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser(sellOrder.UserId));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_WhenBuyOrderNotFound()
    {
        // Arrange
        TradeProposal proposal = CreateTradeProposal(Guid.NewGuid());
        _orderRepositoryMock.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_WhenSellerNotFound()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        Order sellOrder = CreateOrder(userId: Guid.NewGuid(), stockId: buyOrder.StockId);
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, sellOrder.Id);

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(sellOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sellOrder);
        _userRepositoryMock.Setup(x => x.GetAsync(buyOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser(buyOrder.UserId));
        _userRepositoryMock.Setup(x => x.GetAsync(sellOrder.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_WhenSellOrderNotFound()
    {
        // Arrange
        Order buyOrder = CreateOrder();
        TradeProposal proposal = CreateTradeProposal(buyOrder.StockId, buyOrder.Id, Guid.NewGuid());

        _orderRepositoryMock.Setup(x => x.GetAsync(buyOrder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(buyOrder);
        _orderRepositoryMock.Setup(x => x.GetAsync(It.Is<Guid>(id => id != buyOrder.Id), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tradeExecutor.ExecuteAsync(proposal, CancellationToken.None)
        );
    }
}
