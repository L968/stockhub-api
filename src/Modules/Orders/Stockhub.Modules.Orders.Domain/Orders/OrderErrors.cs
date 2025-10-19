using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error CannotCancel = Error.Conflict(
        "Order.CannotCancel",
        "Order cannot be cancelled in its current status."
    );
}
