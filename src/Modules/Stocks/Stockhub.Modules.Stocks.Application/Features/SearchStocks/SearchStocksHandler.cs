using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;

namespace Stockhub.Modules.Stocks.Application.Features.SearchStocks;

internal sealed class SearchStocksHandler(
    IStocksDbContext dbContext,
    ILogger<SearchStocksHandler> logger
) : IRequestHandler<SearchStocksQuery, Result<List<SearchStocksResponse>>>
{
    public async Task<Result<List<SearchStocksResponse>>> Handle(SearchStocksQuery request, CancellationToken cancellationToken)
    {
        List<SearchStocksResponse> stocks = await dbContext.Stocks
            .AsNoTracking()
            .Where(s =>
                s.Symbol.Contains(request.SearchTerm) ||
                s.Name.Contains(request.SearchTerm))
            .Select(s => new SearchStocksResponse(
                s.Id,
                s.Symbol,
                s.Name))
            .ToListAsync(cancellationToken);

        logger.LogDebug("Found {Count} stocks for search term {SearchTerm}", stocks.Count, request.SearchTerm);

        return Result.Success(stocks);
    }
}
