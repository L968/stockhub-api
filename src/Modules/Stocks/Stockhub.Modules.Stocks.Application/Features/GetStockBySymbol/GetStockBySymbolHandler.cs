using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;
using Stockhub.Modules.Stocks.Domain;

namespace Stockhub.Modules.Stocks.Application.Features.GetStockBySymbol;

internal sealed class GetStockBySymbolHandler(
    IStocksDbContext dbContext,
    ILogger<GetStockBySymbolHandler> logger
) : IRequestHandler<GetStockBySymbolQuery, Result<GetStockBySymbolResponse>>
{
    public async Task<Result<GetStockBySymbolResponse>> Handle(GetStockBySymbolQuery request, CancellationToken cancellationToken)
    {
        Stock? stock = await dbContext.Stocks
            .AsNoTracking()
            .Include(s => s.Snapshot)
            .FirstOrDefaultAsync(s => s.Symbol == request.Symbol, cancellationToken);

        if (stock is null || stock.Snapshot is null)
        {
            return Result.Failure<GetStockBySymbolResponse>(StockErrors.SymbolNotFound(request.Symbol));
        }

        var response = new GetStockBySymbolResponse(
            stock.Id,
            stock.Symbol,
            stock.Name,
            stock.Snapshot.LastPrice,
            stock.Snapshot.ChangePercent,
            stock.Snapshot.MinPrice,
            stock.Snapshot.MaxPrice,
            stock.Snapshot.Volume,
            stock.Snapshot.UpdatedAtUtc
        );

        logger.LogDebug("Retrieved stock {@Stock}", response);

        return Result.Success(response);
    }
}
