using Stockhub.Common.Domain;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Domain.Stocks;

namespace Stockhub.Modules.Orders.Domain.Orders;

public sealed class Order : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid StockId { get; private set; }
    public OrderSide Side { get; private set; }
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
    public int FilledQuantity { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Stock Stock { get; private set; }

    private Order() { }

    public Order(Guid userId, Guid stockId, OrderSide side, decimal price, int quantity)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        StockId = stockId;
        Side = side;
        Price = price;
        Quantity = quantity;
        FilledQuantity = 0;
        IsCancelled = false;
    }

    public OrderStatus Status => IsCancelled switch
    {
        true => OrderStatus.Cancelled,
        _ when FilledQuantity == 0 => OrderStatus.Pending,
        _ when FilledQuantity < Quantity => OrderStatus.PartiallyFilled,
        _ => OrderStatus.Filled
    };

    public Result Cancel()
    {
        if (Status == OrderStatus.Filled || Status == OrderStatus.Cancelled)
        {
            return Result.Failure(OrderErrors.CannotCancel);
        }

        IsCancelled = true;
        return Result.Success();
    }
}
