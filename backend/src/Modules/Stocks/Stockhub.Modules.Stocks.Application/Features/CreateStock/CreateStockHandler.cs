using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;
using Stockhub.Modules.Stocks.Domain;

namespace Stockhub.Modules.Stocks.Application.Features.CreateStock;

internal sealed class CreateStockHandler(
    IStocksDbContext dbContext,
    ILogger<CreateStockHandler> logger
) : IRequestHandler<CreateStockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateStockCommand request, CancellationToken cancellationToken)
    {
        bool symbolExists = await dbContext.Stocks.AnyAsync(s => s.Symbol == request.Symbol, cancellationToken);

        if (symbolExists)
        {
            return Result.Failure(StockErrors.StockAlreadyExists(request.Symbol));
        }

        Stock stock = new(request.Symbol, request.Name, request.Sector);
        var snapshot = new StockSnapshot(
            stock.Id,
            lastPrice: 0,
            changePercent: 0,
            minPrice: 0,
            maxPrice: 0,
            volume: 0,
            createdAtUtc: DateTime.UtcNow,
            updatedAtUtc: DateTime.UtcNow
        );

        await dbContext.Stocks.AddAsync(stock, cancellationToken);
        await dbContext.StockSnapshots.AddAsync(snapshot, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Created new stock {@Stock}", stock);

        return Result.Success(stock.Id);
    }
}
