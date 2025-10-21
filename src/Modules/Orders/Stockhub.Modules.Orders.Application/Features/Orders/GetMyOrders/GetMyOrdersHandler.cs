using Stockhub.Common.Application;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Application.Features.Orders.GetMyOrders;

internal sealed class GetMyOrdersHandler(
    IOrdersDbContext dbContext,
    ILogger<GetMyOrdersHandler> logger
) : IRequestHandler<GetMyOrdersQuery, Result<PaginatedList<GetMyOrdersResponse>>>
{
    public async Task<Result<PaginatedList<GetMyOrdersResponse>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<PaginatedList<GetMyOrdersResponse>>(UserErrors.NotFound(request.UserId));
        }

        IQueryable<Order> query = BuildOrdersQuery(request);

        long totalItems = await query.LongCountAsync(cancellationToken);

        List<GetMyOrdersResponse> items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => MapToResponse(o))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<GetMyOrdersResponse>(
            request.Page,
            request.PageSize,
            totalItems,
            items
        );

        logger.LogDebug("Retrieved {Count} orders for user {UserId}", items.Count, request.UserId);

        return Result.Success(paginated);
    }

    private IQueryable<Order> BuildOrdersQuery(GetMyOrdersQuery request)
    {
        IQueryable<Order> query = dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Stock)
            .Where(o => o.UserId == request.UserId);

        if (request.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc <= request.EndDate.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        return query;
    }

    private static GetMyOrdersResponse MapToResponse(Order o)
        => new(
            o.Id,
            o.Side.ToString(),
            o.Price,
            o.Quantity,
            o.FilledQuantity,
            o.Status.ToString(),
            o.CreatedAtUtc,
            o.UpdatedAtUtc,
            new OrderStockResponse(
                o.Stock.Id,
                o.Stock.Symbol,
                o.Stock.Name
            )
        );
}
