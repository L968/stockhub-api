using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderBookTests
{
    private readonly Guid _stockId = Guid.CreateVersion7();

    private Order CreateOrder(OrderSide side, decimal price, int quantity, int filledQuantity = 0)
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
    public void CommitTrade_Should_Fill_Buy_And_Sell_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(buyOrder);
        book.Add(sellOrder);

        var trade = new Trade(
            stockId: _stockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.Id,
            sellOrderId: sellOrder.Id,
            price: 100,
            quantity: 6
        );

        // Act
        book.CommitTrade(trade);

        // Assert
        Assert.Equal(6, buyOrder.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, buyOrder.Status);
        Assert.Equal(6, sellOrder.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sellOrder.Status);
    }

    [Fact]
    public void CommitTrade_Should_Remove_Filled_Orders_From_OrderBook()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(buyOrder);
        book.Add(sellOrder);

        var trade = new Trade(
            stockId: _stockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.Id,
            sellOrderId: sellOrder.Id,
            price: 100,
            quantity: 10
        );

        // Act
        book.CommitTrade(trade);

        // Assert
        Assert.True(book.IsEmpty);
    }

    [Fact]
    public void CommitTrade_Should_Not_Remove_Partially_Filled_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(buyOrder);
        book.Add(sellOrder);

        var trade = new Trade(
            stockId: _stockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.Id,
            sellOrderId: sellOrder.Id,
            price: 100,
            quantity: 5
        );

        // Act
        book.CommitTrade(trade);

        // Assert
        Assert.Equal(5, buyOrder.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, buyOrder.Status);
        Assert.Equal(5, sellOrder.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, sellOrder.Status);
        Assert.Equal(2, book.TotalOrders);
    }

    [Fact]
    public void CommitTrade_Should_Not_Fill_Cancelled_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        buyOrder.Cancel();
        book.Add(buyOrder);
        book.Add(sellOrder);

        var trade = new Trade(
            stockId: _stockId,
            buyerId: buyOrder.UserId,
            sellerId: sellOrder.UserId,
            buyOrderId: buyOrder.Id,
            sellOrderId: sellOrder.Id,
            price: 100,
            quantity: 5
        );

        // Act
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => book.CommitTrade(trade));

        // Assert
        Assert.Equal("Cannot fill a cancelled order.", ex.Message);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(OrderStatus.Cancelled, buyOrder.Status);
        Assert.Equal(0, sellOrder.FilledQuantity);
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
    public void IsEmpty_Should_Return_True_When_No_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);

        // Act & Assert
        Assert.True(book.IsEmpty);
    }

    [Fact]
    public void ProposeTrades_BuyOrder_PartialFill_When_SellQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 5);
        book.Add(sellOrder);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_BuyOrder_Should_Fill_Earliest_Sell_When_Quantity_Limited()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5);
        sellOrder1.CreatedAtUtc = DateTime.UtcNow;
        sellOrder2.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1);
        book.Add(sellOrder1);
        book.Add(sellOrder2);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 5);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_BuyOrder_Should_Fill_Multiple_Sells_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 101, 5);
        book.Add(sellOrder1);
        book.Add(sellOrder2);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 8);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_BuyOrder_Should_Fill_Sells_By_LowestPrice_First()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 98, 10);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sellOrder1);
        book.Add(sellOrder2);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 15);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_BuyOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 95, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder3 = CreateOrder(OrderSide.Sell, 105, 5);
        book.Add(sellOrder1);
        book.Add(sellOrder2);
        book.Add(sellOrder3);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_BuyOrder_Should_Skip_Already_Filled_Sell_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrderFilled = CreateOrder(OrderSide.Sell, 100, 10, 10);
        Order sellOrderPending = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sellOrderFilled);
        book.Add(sellOrderPending);

        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrderPending.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_BuyOrder_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder1 = CreateOrder(OrderSide.Sell, 100, 5);
        Order sellOrder2 = CreateOrder(OrderSide.Sell, 100, 5);
        book.Add(sellOrder1);
        book.Add(sellOrder2);

        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 5);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder1.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_BuyOrder_With_HigherPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);
        book.Add(sellOrder);
        Order buyOrder = CreateOrder(OrderSide.Buy, 105, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_BuyOrder_With_LowerPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder = CreateOrder(OrderSide.Sell, 110, 10);
        book.Add(sellOrder);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

        // Assert
        Assert.Empty(proposals);
    }

    [Fact]
    public void ProposeTrades_SellOrder_PartialFill_When_BuyQuantity_Smaller()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 5);
        book.Add(buyOrder);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_SellOrder_Should_Fill_Multiple_Buys_Correctly()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 105, 4);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 104, 6);
        book.Add(buyOrder1);
        book.Add(buyOrder2);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

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
    public void ProposeTrades_SellOrder_Should_Respect_Price_Limit()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 105, 5);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buyOrder3 = CreateOrder(OrderSide.Buy, 95, 5);
        book.Add(buyOrder1);
        book.Add(buyOrder2);
        book.Add(buyOrder3);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Equal(2, proposals.Count);
        Assert.Equal(100, proposals[0].Price);
        Assert.Equal(100, proposals[1].Price);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(5, proposals[1].Quantity);
    }

    [Fact]
    public void ProposeTrades_SellOrder_Should_Skip_Already_Filled_Buy_Orders()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order filledBuy = CreateOrder(OrderSide.Buy, 100, 10, 10);
        Order pendingBuy = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(filledBuy);
        book.Add(pendingBuy);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(pendingBuy.Id, proposals[0].BuyOrderId);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
    }

    [Fact]
    public void ProposeTrades_SellOrder_Should_Stop_When_Incoming_Order_Is_Filled()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder1 = CreateOrder(OrderSide.Buy, 100, 5);
        Order buyOrder2 = CreateOrder(OrderSide.Buy, 100, 5);
        book.Add(buyOrder1);
        book.Add(buyOrder2);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 5);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(5, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder1.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_SellOrder_With_HigherPrice_Should_Not_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 90, 10);
        book.Add(buyOrder);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Empty(proposals);
    }

    [Fact]
    public void ProposeTrades_SellOrder_With_LowerPrice_Should_Create_Trade()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(buyOrder);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_Should_Create_Trade_With_Correct_TradeData()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 1);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 1);
        book.Add(sellOrder);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

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
    public void ProposeTrades_Should_Use_SellOrder_Price()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order sellOrder = CreateOrder(OrderSide.Sell, 95, 10);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100, 10);
        book.Add(sellOrder);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(buyOrder);

        // Assert
        Assert.Single(proposals);
        Assert.Equal(95, proposals[0].Price);
        Assert.Equal(10, proposals[0].Quantity);
        Assert.Equal(buyOrder.Id, proposals[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, proposals[0].SellOrderId);
    }

    [Fact]
    public void ProposeTrades_With_No_Eligible_Match_Should_Not_Change_Status()
    {
        // Arrange
        var book = new OrderBook(_stockId);
        Order buyOrder = CreateOrder(OrderSide.Buy, 90, 5);
        book.Add(buyOrder);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100, 5);

        // Act
        List<TradeProposal> proposals = book.ProposeTrades(sellOrder);

        // Assert
        Assert.Empty(proposals);
        Assert.Equal(0, buyOrder.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, buyOrder.Status);
        Assert.Equal(0, sellOrder.FilledQuantity);
        Assert.Equal(OrderStatus.Pending, sellOrder.Status);
    }
}
