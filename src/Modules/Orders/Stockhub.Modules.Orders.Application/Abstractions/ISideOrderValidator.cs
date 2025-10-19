using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Application.Abstractions;

public interface ISideOrderValidator
{
    OrderSide Side { get; }

    Task<Result> ValidateAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
}
