using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;

namespace Stockhub.Modules.Stocks.Application.Features.FindStocks;

internal sealed class FindStocksHandler(
    IStocksDbContext dbContext,
    ILogger<FindStocksHandler> logger
) : IRequestHandler<FindStocksQuery, Result<List<FindStocksResponse>>>
{
    public async Task<Result<List<FindStocksResponse>>> Handle(FindStocksQuery request, CancellationToken cancellationToken)
    {
        string query = $"%{request.Query.ToLowerInvariant().Trim()}%";

        List<FindStocksResponse> stocks = await dbContext.Stocks
            .AsNoTracking()
            .Where(s =>
                EF.Functions.ILike(s.Symbol, query) ||
                EF.Functions.ILike(s.Name, query))
            .Select(s => new FindStocksResponse(
                s.Id,
                s.Symbol,
                s.Name))
            .ToListAsync(cancellationToken);

        logger.LogDebug("Found {Count} stocks for search term {SearchTerm}", stocks.Count, request.Query);

        return Result.Success(stocks);
    }
}
