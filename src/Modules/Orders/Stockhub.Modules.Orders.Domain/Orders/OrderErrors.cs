using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error CannotCancel = Error.Conflict(
        "Order.CannotCancel",
        "Order cannot be cancelled in its current status."
    );

    public static readonly Error InsufficientBalance = Error.Problem(
        "Order.InsufficientBalance",
        "The user does not have enough balance to place this buy order."
    );

    public static readonly Error InsufficientPortfolio = Error.Problem(
        "Order.InsufficientPortfolio",
        "The user does not have enough quantity in portfolio to place this sell order."
    );

    public static readonly Error ValidatorNotFound = Error.Failure(
        "Order.ValidatorNotFound",
        "No validation strategy was found for the specified order side."
    );
}
