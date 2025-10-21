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
        Stock stock = new(request.Symbol, request.Name, request.Sector);

        await dbContext.Stocks.AddAsync(stock, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Created new stock {@Stock}", stock);

        return Result.Success(stock.Id);
    }
}
