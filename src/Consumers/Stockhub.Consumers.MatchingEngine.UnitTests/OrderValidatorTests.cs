using Moq;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderValidatorTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly OrderValidator _validator;

    public OrderValidatorTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _validator = new OrderValidator(_userRepositoryMock.Object);
    }

    private Order CreateOrder(
        decimal price = 10,
        int quantity = 5,
        OrderSide side = OrderSide.Buy,
        bool isCancelled = false,
        Guid? userId = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            StockId = Guid.NewGuid(),
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = 0,
            IsCancelled = isCancelled,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Should_Fail_When_PriceIsZero()
    {
        // Arrange
        Order order = CreateOrder(price: 0);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Should_Fail_When_QuantityIsZero()
    {
        // Arrange
        Order order = CreateOrder(quantity: 0);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task Should_Fail_When_OrderIsCancelled()
    {
        // Arrange
        Order order = CreateOrder(isCancelled: true);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IsCancelled");
    }

    [Fact]
    public async Task Should_Pass_When_SellOrder_WithAnyBalance()
    {
        // Arrange
        Order order = CreateOrder(side: OrderSide.Sell);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Should_Fail_When_BuyOrder_WithInsufficientBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Order order = CreateOrder(side: OrderSide.Buy, userId: userId);
        _userRepositoryMock
            .Setup(r => r.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("enough balance"));
    }

    [Fact]
    public async Task Should_Pass_When_BuyOrder_WithSufficientBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Order order = CreateOrder(side: OrderSide.Buy, userId: userId);
        _userRepositoryMock
            .Setup(r => r.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Should_Fail_When_MultipleRulesViolated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Order order = CreateOrder(price: 0, quantity: 0, isCancelled: true, side: OrderSide.Buy, userId: userId);
        _userRepositoryMock
            .Setup(r => r.HasSufficientBalanceAsync(userId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
        Assert.Contains(result.Errors, e => e.PropertyName == "IsCancelled");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("enough balance"));
    }

    [Fact]
    public async Task Should_Call_HasSufficientBalance_For_BuyOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Order order = CreateOrder(side: OrderSide.Buy, userId: userId);
        _userRepositoryMock
            .Setup(r => r.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _validator.ValidateAsync(order);

        // Assert
        _userRepositoryMock.Verify(r => r.HasSufficientBalanceAsync(userId, order.Price * order.Quantity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_HasSufficientBalance_For_SellOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Order order = CreateOrder(side: OrderSide.Sell, userId: userId);

        // Act
        await _validator.ValidateAsync(order);

        // Assert
        _userRepositoryMock.Verify(r => r.HasSufficientBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
