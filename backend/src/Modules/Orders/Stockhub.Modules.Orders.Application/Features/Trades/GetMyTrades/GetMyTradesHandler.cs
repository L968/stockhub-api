using Stockhub.Common.Application;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Trades;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Application.Features.Trades.GetMyTrades;

internal sealed class GetMyTradesHandler(
    IOrdersDbContext dbContext,
    ILogger<GetMyTradesHandler> logger
) : IRequestHandler<GetMyTradesQuery, Result<PaginatedList<GetMyTradesResponse>>>
{
    public async Task<Result<PaginatedList<GetMyTradesResponse>>> Handle(GetMyTradesQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        IQueryable<Trade> query = BuildTradesQuery(request);

        long totalItems = await query.LongCountAsync(cancellationToken);

        List<GetMyTradesResponse> items = await query
            .OrderByDescending(t => t.ExecutedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => MapToResponse(t, request.UserId))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<GetMyTradesResponse>(
            request.Page,
            request.PageSize,
            totalItems,
            items
        );

        logger.LogDebug("Retrieved {Count} trades for user {UserId}", items.Count, request.UserId);

        return Result.Success(paginated);
    }

    private IQueryable<Trade> BuildTradesQuery(GetMyTradesQuery request)
    {
        IQueryable<Trade> query = dbContext.Trades
            .AsNoTracking()
            .Include(t => t.Stock)
            .Where(t => t.BuyerId == request.UserId || t.SellerId == request.UserId);

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.ExecutedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.ExecutedAt <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            query = query.Where(t => t.Stock.Symbol == request.Symbol);
        }

        return query;
    }

    private static GetMyTradesResponse MapToResponse(Trade t, Guid userId)
        => new(
            t.Id,
            t.Stock.Symbol,
            t.BuyerId == userId ? "BUY" : "SELL",
            t.Price,
            t.Quantity,
            t.BuyerId == userId ? t.BuyOrderId : t.SellOrderId,
            t.ExecutedAt
        );
}
