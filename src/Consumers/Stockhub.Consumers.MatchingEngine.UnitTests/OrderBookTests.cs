using OpenTelemetry.Trace;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Events.OrderPlaced;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Xunit;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderBookTests
{
    private readonly Guid _stockId = Guid.NewGuid();

    private OrderPlacedEvent CreateOrder(
        OrderSide side,
        decimal price,
        int quantity,
        int filledQuantity = 0,
        OrderStatus status = OrderStatus.Pending
        )
    {
        return new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StockId = _stockId,
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = filledQuantity,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public void Add_Order_Should_Increase_TotalOrders_And_Set_IsEmpty_False()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 10);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 101, 5);

        // Act
        book.Add(buy);
        book.Add(sell);

        // Assert
        Assert.Equal(2, book.TotalOrders);
        Assert.False(book.IsEmpty);
    }

    [Fact]
    public void IsEmpty_Should_Return_True_When_No_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);

        // Act & Assert
        Assert.True(book.IsEmpty);
    }

    [Fact]
    public void Match_BuyOrder_With_HigherPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sell);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 105, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Trade trade = trades[0];
        Assert.Equal(10, trade.Quantity);
        Assert.Equal(100, trade.Price);
        Assert.Equal(10, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
        Assert.Equal(10, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);

        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.OrderId, trade.BuyOrderId);
        Assert.Equal(sell.OrderId, trade.SellOrderId);
        Assert.Equal(_stockId, trade.StockId);
    }

    [Fact]
    public void Match_BuyOrder_With_LowerPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 110, 10);
        book.Add(sell);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Empty(trades);
        Assert.Equal(0, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, buy.Status);
        Assert.Equal(0, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sell.Status);
    }

    [Fact]
    public void Match_SellOrder_With_LowerPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(buy);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(10, trades[0].Quantity);
        Assert.Equal(95, trades[0].Price);
        Assert.Equal(10, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(10, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
    }

    [Fact]
    public void Match_SellOrder_With_HigherPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 90, 10);
        book.Add(buy);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Empty(trades);
        Assert.Equal(0, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, buy.Status);
        Assert.Equal(0, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sell.Status);
    }

    [Fact]
    public void Match_BuyOrder_PartialFill_When_SellQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 5);
        book.Add(sell);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(5, trades[0].Quantity);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, buy.Status);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
    }

    [Fact]
    public void Match_SellOrder_PartialFill_When_BuyQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 5);
        book.Add(buy);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(5, trades[0].Quantity);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sell.Status);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
    }

    [Fact]
    public void Match_BuyOrder_Should_Fill_Multiple_Sells_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell1 = CreateOrder(OrderSide.Sell, 100, 5);
        OrderPlacedEvent sell2 = CreateOrder(OrderSide.Sell, 101, 5);
        book.Add(sell1);
        book.Add(sell2);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 105, 8);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Equal(2, trades.Count);
        Assert.Equal(5, trades[0].Quantity);
        Assert.Equal(3, trades[1].Quantity);
        Assert.Equal(8, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
        Assert.Equal(5, sell1.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell1.Status);
        Assert.Equal(3, sell2.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sell2.Status);
    }

    [Fact]
    public void Match_SellOrder_Should_Fill_Multiple_Buys_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy1 = CreateOrder(OrderSide.Buy, 105, 4);
        OrderPlacedEvent buy2 = CreateOrder(OrderSide.Buy, 104, 6);
        book.Add(buy1);
        book.Add(buy2);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Equal(2, trades.Count);
        Assert.Equal(4, trades[0].Quantity);
        Assert.Equal(6, trades[1].Quantity);
        Assert.Equal(10, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(4, buy1.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy1.Status);
        Assert.Equal(6, buy2.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy2.Status);
    }

    [Fact]
    public void Match_With_No_Eligible_Match_Should_Not_Change_Status()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 90, 5);
        book.Add(buy);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 5);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Empty(trades);
        Assert.Equal(0, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, buy.Status);
        Assert.Equal(0, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sell.Status);
    }

    [Fact]
    public void Match_BuyOrder_Should_Fill_Sells_By_LowestPrice_First()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell1 = CreateOrder(OrderSide.Sell, 98, 10);
        OrderPlacedEvent sell2 = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sell1);
        book.Add(sell2);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 15);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Equal(2, trades.Count);
        Assert.Equal(98, trades[0].Price);
        Assert.Equal(100, trades[1].Price);
        Assert.Equal(10, sell1.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell1.Status);
        Assert.Equal(5, sell2.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sell2.Status);
        Assert.Equal(15, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
    }

    [Fact]
    public void Match_Should_Create_Trade_With_Correct_TradeData()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 100, 1);
        OrderPlacedEvent buy = CreateOrder(OrderSide.Buy, 100, 1);
        book.Add(sell);

        // Act
        Trade trade = book.Match(buy).Single();

        // Assert
        Assert.Equal(_stockId, trade.StockId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(1, trade.Quantity);
        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.OrderId, trade.BuyOrderId);
        Assert.Equal(sell.OrderId, trade.SellOrderId);
    }

    [Fact]
    public void RemoveFilledOrders_Should_Remove_Multiple_Filled_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        OrderPlacedEvent buy1 = CreateOrder(OrderSide.Buy, 100, 0, 0, OrderStatus.Filled);
        OrderPlacedEvent buy2 = CreateOrder(OrderSide.Buy, 101, 5, 5, OrderStatus.Filled);
        OrderPlacedEvent sell = CreateOrder(OrderSide.Sell, 102, 10);
        book.Add(buy1);
        book.Add(buy2);
        book.Add(sell);

        // Act
        book.RemoveFilledOrders();

        // Assert
        Assert.Equal(1, book.TotalOrders);
        Assert.Equal(OrderStatus.Pending, sell.Status);
    }
}
