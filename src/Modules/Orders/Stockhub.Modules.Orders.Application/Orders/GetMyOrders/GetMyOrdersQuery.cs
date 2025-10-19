using Stockhub.Common.Application;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Application.Orders.GetMyOrders;

public sealed record GetMyOrdersQuery(
    Guid UserId,
    int Page,
    int PageSize,
    DateTime? StartDate,
    DateTime? EndDate,
    OrderStatus? Status
) : IRequest<Result<PaginatedList<GetMyOrdersResponse>>>;
