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
        int filledQuantity = 0,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null
    )
    {
        DateTime now = DateTime.UtcNow;

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
            CreatedAtUtc = createdAtUtc ?? now,
            UpdatedAtUtc = updatedAtUtc ?? now
        };
    }

    [Fact]
    public void Constructor_Should_Set_StockId_Correctly()
    {
        // Arrange & Act
        var orderBook = new OrderBook(_stockId, []);

        // Assert
        Assert.Equal(0, orderBook.Count);
    }

    [Fact]
    public void ProposeAllPossibleTrades_BuyOrder_PartialFill_When_SellQuantity_Smaller()
    {
        // Arrange
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 5);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        var orderBook = new OrderBook(_stockId, [sellOrder, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        TradeProposal proposal = proposals[0];

        Assert.Equal(5, proposal.Quantity);
        Assert.Equal(100, proposal.Price);
        Assert.Equal(buyOrder.Id, proposal.BuyOrderId);
        Assert.Equal(sellOrder.Id, proposal.SellOrderId);
        Assert.Equal(_stockId, proposal.StockId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_BuyOrder_Should_Fill_Earliest_Sell_When_Quantity_Limited()
    {
        // Arrange
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5, createdAtUtc: DateTime.UtcNow);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5, createdAtUtc: DateTime.UtcNow.AddMinutes(-1));
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 5);

        var orderBook = new OrderBook(_stockId, [sellOrder1, sellOrder2, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        TradeProposal proposal = proposals[0];

        Assert.Equal(5, proposal.Quantity);
        Assert.Equal(100, proposal.Price);
        Assert.Equal(buyOrder.Id, proposal.BuyOrderId);
        Assert.Equal(sellOrder2.Id, proposal.SellOrderId);
        Assert.Equal(_stockId, proposal.StockId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_BuyOrder_Should_Fill_Multiple_Sells_Correctly()
    {
        // Arrange
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 101, 5);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 8);

        var orderBook = new OrderBook(_stockId, [sellOrder1, sellOrder2, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Equal(2, proposals.Count);

        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder1.Id, proposals[0].SellOrderId);
        Assert.Equal(_stockId, proposals[0].StockId);

        Assert.Equal(3, proposals[1].Quantity);
        Assert.Equal(101, proposals[1].Price);
        Assert.Equal(buyOrder.Id, proposals[1].BuyOrderId);
        Assert.Equal(sellOrder2.Id, proposals[1].SellOrderId);
        Assert.Equal(_stockId, proposals[1].StockId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_BuyOrder_Should_Fill_Sells_By_LowestPrice_First()
    {
        // Arrange
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 98, 10);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 15);

        var orderBook = new OrderBook(_stockId, [sellOrder1, sellOrder2, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Equal(2, proposals.Count);

        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(98, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder1.Id, proposals[0].SellOrderId);
        Assert.Equal(_stockId, proposals[0].StockId);

        Assert.Equal(5, proposals[1].Quantity);
        Assert.Equal(100, proposals[1].Price);
        Assert.Equal(buyOrder.Id, proposals[1].BuyOrderId);
        Assert.Equal(sellOrder2.Id, proposals[1].SellOrderId);
        Assert.Equal(_stockId, proposals[1].StockId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_BuyOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 95, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder3 = CreateOrder(OrderSide.Sell, 105, 5);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);

        var orderBook = new OrderBook(_stockId, [sellOrder1, sellOrder2, sellOrder3, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Equal(2, proposals.Count);

        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder1.Id, proposals[0].SellOrderId);
        Assert.Equal(_stockId, proposals[0].StockId);

        Assert.Equal(5, proposals[1].Quantity);
        Assert.Equal(100, proposals[1].Price);
        Assert.Equal(buyOrder.Id, proposals[1].BuyOrderId);
        Assert.Equal(sellOrder2.Id, proposals[1].SellOrderId);
        Assert.Equal(_stockId, proposals[1].StockId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_PartialFill_When_BuyQuantity_Smaller()
    {
        // Arrange
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 5);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);

        var orderBook = new OrderBook(_stockId, [buyOrder, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_Should_Fill_Multiple_Buys_Correctly()
    {
        // Arrange
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 105, 4);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 104, 6);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        var orderBook = new OrderBook(_stockId, [buyOrder1, buyOrder2, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Equal(2, proposals.Count);

        Assert.Equal(4, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder1.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);

        Assert.Equal(6, proposals[1].Quantity);
        Assert.Equal(100, proposals[1].Price);
        Assert.Equal(buyOrder2.Id, proposals[1].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[1].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 105, 5);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buyOrder3 = CreateOrder(OrderSide.Buy, 95, 5);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        var orderBook = new OrderBook(_stockId, [buyOrder1, buyOrder2, buyOrder3, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Equal(2, proposals.Count);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(100, proposals[1].Price);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(5, proposals[1].Quantity);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_Should_Skip_Already_Filled_Buy_Orders()
    {
        // Arrange
        Order filledBuy = CreateOrder(OrderSide.Buy, 100, 10, 10);
        Order pendingBuy = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);

        var orderBook = new OrderBook(_stockId, [filledBuy, pendingBuy, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(pendingBuy.Id, proposals[0].BuyOrderId);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 100, 5);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 5);

        var orderBook = new OrderBook(_stockId, [buyOrder1, buyOrder2, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder1.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_With_HigherPrice_Should_Not_Create_Trade()
    {
        // Arrange
        Order buyOrder = CreateOrder(OrderSide.Buy, 90, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        var orderBook = new OrderBook(_stockId, [buyOrder, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Empty(proposals);
    }

    [Fact]
    public void ProposeAllPossibleTrades_SellOrder_With_LowerPrice_Should_Create_Trade()
    {
        // Arrange
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);
        var orderBook = new OrderBook(_stockId, [buyOrder, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_Should_Create_Trade_With_Correct_TradeData()
    {
        // Arrange
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 1);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 1);
        var orderBook = new OrderBook(_stockId, [sellOrder, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        TradeProposal proposal = proposals.Single();
        Assert.Equal(_stockId, proposal.StockId);
        Assert.Equal(100, proposal.Price);
        Assert.Equal(1, proposal.Quantity);
        Assert.Equal(buyOrder.Id, proposal.BuyOrderId);
        Assert.Equal(sellOrder.Id, proposal.SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_Should_Skip_Already_Filled_Sell_Orders()
    {
        // Arrange
        Order sellOrderFilled = CreateOrder(OrderSide.Sell, 100, 10, 10);
        Order sellOrderPending = CreateOrder(OrderSide.Sell, 100, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 10);

        var orderBook = new OrderBook(_stockId, [sellOrderFilled, sellOrderPending, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrderPending.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 5);

        var orderBook = new OrderBook(_stockId, [sellOrder1, sellOrder2, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder1.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_Should_Use_SellOrder_Price()
    {
        // Arrange
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        var orderBook = new OrderBook(_stockId, [sellOrder, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_With_HigherPrice_Should_Create_Trade()
    {
        // Arrange
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 10);

        var orderBook = new OrderBook(_stockId, [sellOrder, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeAllPossibleTrades_With_LowerPrice_Should_Not_Create_Trade()
    {
        // Arrange
        Order sellOrder = CreateOrder(OrderSide.Sell, 110, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);

        var orderBook = new OrderBook(_stockId, [sellOrder, buyOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Empty(proposals);
    }

    [Fact]
    public void ProposeAllPossibleTrades_With_No_Eligible_Match_Should_Not_Change_Status()
    {
        // Arrange
        Order buyOrder = CreateOrder(OrderSide.Buy, 90, 5);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 5);
        var orderBook = new OrderBook(_stockId, [buyOrder, sellOrder]);

        // Act
        List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

        // Assert
        Assert.Empty(proposals);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, buyOrder.Status);
        Assert.Equal(0, sellOrder.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sellOrder.Status);
    }
}
