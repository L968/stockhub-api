using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.UnitTests;

public class OrderTests
{
    [Fact]
    public void NewOrder_ShouldHavePendingStatus()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StockId = Guid.NewGuid(),
            Side = OrderSide.Buy,
            Price = 100m,
            Quantity = 10,
            FilledQuantity = 0,
            IsCancelled = false
        };

        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Fill_ShouldIncreaseFilledQuantity_AndUpdateStatus()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 0 };

        order.Fill(4);
        Assert.Equal(4, order.FilledQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);

        order.Fill(6);
        Assert.Equal(10, order.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void Fill_MoreThanQuantity_ShouldNotExceedQuantity()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 0 };

        order.Fill(15);

        Assert.Equal(10, order.FilledQuantity);
        Assert.Equal(OrderStatus.Filled, order.Status);
    }

    [Fact]
    public void Cancel_ShouldSetIsCancelled_AndUpdateStatus()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 0 };

        order.Cancel();

        Assert.True(order.IsCancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldNotThrow()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 0, IsCancelled = true };

        Exception ex = Record.Exception( order.Cancel);
        Assert.Null(ex);
        Assert.True(order.IsCancelled);
    }

    [Fact]
    public void Cancel_FilledOrder_ShouldThrow()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 10 };

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }

    [Fact]
    public void Fill_CancelledOrder_ShouldThrow()
    {
        var order = new Order { Quantity = 10, FilledQuantity = 0, IsCancelled = true };

        Assert.Throws<InvalidOperationException>(() => order.Fill(5));
    }
}
