namespace Stockhub.Modules.Orders.Domain.Orders;

public enum OrderStatus
{
    Pending = 0,
    PartiallyFilled = 1,
    Filled = 2,
    Cancelled = 3
}
