using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;

namespace Stockhub.Modules.Stocks.Application.Features.GetLastPrices;

internal sealed class GetLastPricesHandler(
    IStocksDbContext dbContext,
    ILogger<GetLastPricesHandler> logger
) : IRequestHandler<GetLastPricesQuery, Result<GetLastPricesResponse>>
{
    public async Task<Result<GetLastPricesResponse>> Handle(GetLastPricesQuery request, CancellationToken cancellationToken)
    {
        var symbols = request.Symbols
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (symbols.Count == 0)
        {
            return Result.Success(new GetLastPricesResponse([]));
        }

        var data = await dbContext.Stocks
            .AsNoTracking()
            .Include(s => s.Snapshot)
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.Snapshot.LastPrice })
            .ToListAsync(cancellationToken);

        var prices = data.ToDictionary(
            x => x.Symbol,
            x => x.LastPrice,
            StringComparer.OrdinalIgnoreCase
        );

        logger.LogDebug("Fetched last prices for {Count} stocks", prices.Count);

        return Result.Success(new GetLastPricesResponse(prices));
    }
}
