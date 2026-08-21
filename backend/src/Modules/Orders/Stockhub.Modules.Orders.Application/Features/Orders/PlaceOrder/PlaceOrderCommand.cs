using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Application.Features.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid UserId,
    Guid StockId,
    OrderSide Side,
    decimal Price,
    int Quantity
) : IRequest<Result<Guid>>;
