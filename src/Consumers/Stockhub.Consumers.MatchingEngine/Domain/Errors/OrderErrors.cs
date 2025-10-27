using Stockhub.Common.Domain.Results;

namespace Stockhub.Consumers.MatchingEngine.Domain.Errors;

internal static class OrderErrors
{
    public static readonly Error InsufficientBalance = Error.Conflict(
        "Order.InsufficientBalance",
        "The user does not have enough balance to place this buy order."
    );
}
