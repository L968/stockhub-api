using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderBookTests
{
    private readonly Guid _stockId = Guid.CreateVersion7();

    private Order CreateOrder(
        OrderSide side,
        decimal price,
        int quantity,
        int filledQuantity = 0
        )
    {
        return new Order
        {
            Id = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            StockId = _stockId,
            Side = side,
            Price = price,
            Quantity = quantity,
            FilledQuantity = filledQuantity,
            IsCancelled = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public void Add_BuyOrder_Should_Add_To_BuyOrders_List()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        book.Add(buy);

        // Assert
        Assert.Equal(1, book.TotalOrders);
        Assert.False(book.IsEmpty);
    }

    [Fact]
    public void Add_Order_Should_Increase_TotalOrders_And_Set_IsEmpty_False()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);
        Order sell = CreateOrder(OrderSide.Sell, 101, 5);

        // Act
        book.Add(buy);
        book.Add(sell);

        // Assert
        Assert.Equal(2, book.TotalOrders);
        Assert.False(book.IsEmpty);
    }

    [Fact]
    public void Add_SellOrder_Should_Add_To_SellOrders_List()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        book.Add(sell);

        // Assert
        Assert.Equal(1, book.TotalOrders);
        Assert.False(book.IsEmpty);
    }

    [Fact]
    public void Constructor_Should_Set_StockId_Correctly()
    {
        // Arrange & Act
        var book = new OrderBook(_stockId);

        // Assert
        Assert.Equal(_stockId, book.StockId);
        Assert.True(book.IsEmpty);
        Assert.Equal(0, book.TotalOrders);
    }

    [Fact]
    public void CreateTrade_Should_Use_SellOrder_Price()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 95, 10);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(sell);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(95, trades[0].Price);
    }

    [Fact]
    public void GetOrderStatus_Should_Return_PartiallyFilled_When_Partial_Fill()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sell);

        Order buy = CreateOrder(OrderSide.Buy, 105, 5);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sell.Status);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
    }

    [Fact]
    public void GetOrderStatus_Should_Return_Pending_When_No_Filled_Quantity()
    {
        // Arrange
        Order order = CreateOrder(OrderSide.Buy, 100, 10, 0);

        // Act & Assert
        var book = new OrderBook(_stockId);
        book.Add(order);

        Assert.Equal(OrderStatus.Pending, order.Status);
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
    public void Match_BuyOrder_PartialFill_When_SellQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 100, 5);
        book.Add(sell);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Trade trade = trades[0];
        Assert.Equal(5, trade.Quantity);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, buy.Status);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell.Id, trade.SellOrderId);
        Assert.Equal(_stockId, trade.StockId);
    }

    [Fact]
    public void Match_BuyOrder_Should_Fill_Earliest_Sell_When_Quantity_Limited()
    {
        // Arrange
        var book = new OrderBook(_stockId);

        Order sell1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sell2 = CreateOrder(OrderSide.Sell, 100, 5);

        sell1.CreatedAtUtc = DateTime.UtcNow;
        sell2.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1);

        book.Add(sell1);
        book.Add(sell2);

        Order buy = CreateOrder(OrderSide.Buy, 105, 5);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Trade trade = trades[0];

        Assert.Equal(sell2.Id, trade.SellOrderId);
        Assert.Equal(5, trade.Quantity);

        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);

        Assert.Equal(5, sell2.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell2.Status);

        Assert.Equal(0, sell1.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sell1.Status);

        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell2.UserId, trade.SellerId);
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell2.Id, trade.SellOrderId);
    }

    [Fact]
    public void Match_BuyOrder_Should_Fill_Multiple_Sells_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sell2 = CreateOrder(OrderSide.Sell, 101, 5);
        book.Add(sell1);
        book.Add(sell2);
        Order buy = CreateOrder(OrderSide.Buy, 105, 8);

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
        Assert.Equal(buy.UserId, trades[0].BuyerId);
        Assert.Equal(sell1.UserId, trades[0].SellerId);
        Assert.Equal(buy.UserId, trades[1].BuyerId);
        Assert.Equal(sell2.UserId, trades[1].SellerId);
    }

    [Fact]
    public void Match_BuyOrder_Should_Fill_Sells_By_LowestPrice_First()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell1 = CreateOrder(OrderSide.Sell, 98, 10);
        Order sell2 = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sell1);
        book.Add(sell2);
        Order buy = CreateOrder(OrderSide.Buy, 100, 15);

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
        Assert.Equal(buy.UserId, trades[0].BuyerId);
        Assert.Equal(sell1.UserId, trades[0].SellerId);
        Assert.Equal(buy.UserId, trades[1].BuyerId);
        Assert.Equal(sell2.UserId, trades[1].SellerId);
    }

    [Fact]
    public void Match_BuyOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell1 = CreateOrder(OrderSide.Sell, 95, 5);
        Order sell2 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sell3 = CreateOrder(OrderSide.Sell, 105, 5);

        book.Add(sell1);
        book.Add(sell2);
        book.Add(sell3);

        Order buy = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Equal(2, trades.Count);
        Assert.Equal(95, trades[0].Price);
        Assert.Equal(100, trades[1].Price);
        Assert.Equal(10, buy.FilledQuantity);
        Assert.Equal(0, sell3.FilledQuantity);
    }

    [Fact]
    public void Match_BuyOrder_Should_Skip_Already_Filled_Sell_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order filledSell = CreateOrder(OrderSide.Sell, 100, 10, 10);
        Order pendingSell = CreateOrder(OrderSide.Sell, 100, 10);

        book.Add(filledSell);
        book.Add(pendingSell);

        Order buy = CreateOrder(OrderSide.Buy, 105, 10);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(pendingSell.Id, trades[0].SellOrderId);
        Assert.Equal(10, pendingSell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, pendingSell.Status);
        Assert.Equal(10, filledSell.FilledQuantity);
    }

    [Fact]
    public void Match_BuyOrder_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sell2 = CreateOrder(OrderSide.Sell, 100, 5);

        book.Add(sell1);
        book.Add(sell2);

        Order buy = CreateOrder(OrderSide.Buy, 105, 5);

        // Act
        var trades = book.Match(buy).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
        Assert.Equal(5, sell1.FilledQuantity + sell2.FilledQuantity);
    }

    [Fact]
    public void Match_BuyOrder_With_HigherPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sell);
        Order buy = CreateOrder(OrderSide.Buy, 105, 10);

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
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell.Id, trade.SellOrderId);
        Assert.Equal(_stockId, trade.StockId);
    }

    [Fact]
    public void Match_BuyOrder_With_LowerPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 110, 10);
        book.Add(sell);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);

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
    public void Match_SellOrder_PartialFill_When_BuyQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 100, 5);
        book.Add(buy);
        Order sell = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Trade trade = trades[0];
        Assert.Equal(5, trade.Quantity);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sell.Status);
        Assert.Equal(5, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell.Id, trade.SellOrderId);
        Assert.Equal(_stockId, trade.StockId);
    }

    [Fact]
    public void Match_SellOrder_Should_Fill_Multiple_Buys_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy1 = CreateOrder(OrderSide.Buy, 105, 4);
        Order buy2 = CreateOrder(OrderSide.Buy, 104, 6);
        book.Add(buy1);
        book.Add(buy2);
        Order sell = CreateOrder(OrderSide.Sell, 100, 10);

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
        Assert.Equal(buy1.UserId, trades[0].BuyerId);
        Assert.Equal(sell.UserId, trades[0].SellerId);
        Assert.Equal(buy2.UserId, trades[1].BuyerId);
        Assert.Equal(sell.UserId, trades[1].SellerId);
    }

    [Fact]
    public void Match_SellOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy1 = CreateOrder(OrderSide.Buy, 105, 5);
        Order buy2 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buy3 = CreateOrder(OrderSide.Buy, 95, 5);

        book.Add(buy1);
        book.Add(buy2);
        book.Add(buy3);

        Order sell = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Equal(2, trades.Count);
        Assert.Equal(100, trades[0].Price);
        Assert.Equal(100, trades[1].Price);
        Assert.Equal(10, sell.FilledQuantity);
        Assert.Equal(0, buy3.FilledQuantity);
    }

    [Fact]
    public void Match_SellOrder_Should_Skip_Already_Filled_Buy_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order filledBuy = CreateOrder(OrderSide.Buy, 100, 10, 10);
        Order pendingBuy = CreateOrder(OrderSide.Buy, 100, 10);

        book.Add(filledBuy);
        book.Add(pendingBuy);

        Order sell = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(pendingBuy.Id, trades[0].BuyOrderId);
        Assert.Equal(10, pendingBuy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, pendingBuy.Status);
        Assert.Equal(10, filledBuy.FilledQuantity);
    }

    [Fact]
    public void Match_SellOrder_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy1 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buy2 = CreateOrder(OrderSide.Buy, 100, 5);

        book.Add(buy1);
        book.Add(buy2);

        Order sell = CreateOrder(OrderSide.Sell, 95, 5);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Assert.Equal(5, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(5, buy1.FilledQuantity + buy2.FilledQuantity);
    }

    [Fact]
    public void Match_SellOrder_With_HigherPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 90, 10);
        book.Add(buy);
        Order sell = CreateOrder(OrderSide.Sell, 100, 10);

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
    public void Match_SellOrder_With_LowerPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(buy);
        Order sell = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        var trades = book.Match(sell).ToList();

        // Assert
        Assert.Single(trades);
        Trade trade = trades[0];
        Assert.Equal(10, trade.Quantity);
        Assert.Equal(95, trade.Price);
        Assert.Equal(10, sell.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(10, buy.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, buy.Status);
        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell.Id, trade.SellOrderId);
        Assert.Equal(_stockId, trade.StockId);
    }

    [Fact]
    public void Match_Should_Create_Trade_With_Correct_TradeData()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sell = CreateOrder(OrderSide.Sell, 100, 1);
        Order buy = CreateOrder(OrderSide.Buy, 100, 1);
        book.Add(sell);

        // Act
        Trade trade = book.Match(buy).Single();

        // Assert
        Assert.Equal(_stockId, trade.StockId);
        Assert.Equal(100, trade.Price);
        Assert.Equal(1, trade.Quantity);
        Assert.Equal(buy.UserId, trade.BuyerId);
        Assert.Equal(sell.UserId, trade.SellerId);
        Assert.Equal(buy.Id, trade.BuyOrderId);
        Assert.Equal(sell.Id, trade.SellOrderId);
    }

    [Fact]
    public void Match_With_No_Eligible_Match_Should_Not_Change_Status()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buy = CreateOrder(OrderSide.Buy, 90, 5);
        book.Add(buy);
        Order sell = CreateOrder(OrderSide.Sell, 100, 5);

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
    public void RemoveFilledOrders_Should_Remove_Only_Filled_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);

        Order filledBuy = CreateOrder(OrderSide.Buy, 100, 10, 10);
        Order partialBuy = CreateOrder(OrderSide.Buy, 100, 10, 5);
        Order pendingBuy = CreateOrder(OrderSide.Buy, 100, 10, 0);

        Order filledSell = CreateOrder(OrderSide.Sell, 100, 10, 10);
        Order partialSell = CreateOrder(OrderSide.Sell, 100, 10, 5);
        Order pendingSell = CreateOrder(OrderSide.Sell, 100, 10, 0);

        book.Add(filledBuy);
        book.Add(partialBuy);
        book.Add(pendingBuy);
        book.Add(filledSell);
        book.Add(partialSell);
        book.Add(pendingSell);

        // Act
        Order incoming = CreateOrder(OrderSide.Buy, 90, 1);
        book.Match(incoming);

        // Assert
        Assert.Equal(4, book.TotalOrders);
    }
}
