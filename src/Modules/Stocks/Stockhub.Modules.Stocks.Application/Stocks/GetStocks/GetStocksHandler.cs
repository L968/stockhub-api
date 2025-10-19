using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;

namespace Stockhub.Modules.Stocks.Application.Stocks.GetStocks;

internal sealed class GetStocksHandler(
    IStocksDbContext dbContext,
    ILogger<GetStocksHandler> logger
) : IRequestHandler<GetStocksQuery, Result<List<GetStocksResponse>>>
{
    public async Task<Result<List<GetStocksResponse>>> Handle(GetStocksQuery request, CancellationToken cancellationToken)
    {
        List<GetStocksResponse> stocks = await dbContext.Stocks
            .AsNoTracking()
            .Include(s => s.Snapshot)
            .Select(s => new GetStocksResponse(
                s.Id,
                s.Symbol,
                s.Name,
                s.Snapshot.LastPrice,
                s.Snapshot.ChangePercent,
                s.Snapshot.MinPrice,
                s.Snapshot.MaxPrice,
                s.Snapshot.Volume,
                s.Snapshot.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        logger.LogDebug("Retrieved {Count} stocks with latest snapshots from database", stocks.Count);

        return Result.Success(stocks);
    }
}
