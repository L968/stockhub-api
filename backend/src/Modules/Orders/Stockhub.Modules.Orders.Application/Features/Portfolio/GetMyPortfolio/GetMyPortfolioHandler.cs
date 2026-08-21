using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;
using Stockhub.Modules.Orders.Domain.Users;
using Stockhub.Modules.Stocks.PublicApi;

namespace Stockhub.Modules.Orders.Application.Features.Portfolio.GetMyPortfolio;

internal sealed class GetMyPortfolioHandler(
    IOrdersDbContext dbContext,
    IStocksApi stocksApi,
    ILogger<GetMyPortfolioHandler> logger
) : IRequestHandler<GetMyPortfolioQuery, Result<GetMyPortfolioResponse>>
{
    public async Task<Result<GetMyPortfolioResponse>> Handle(GetMyPortfolioQuery request, CancellationToken cancellationToken)
    {
        Result userResult = await EnsureUserExistsAsync(request.UserId, cancellationToken);
        if (userResult.IsFailure)
        {
            return Result.Failure<GetMyPortfolioResponse>(userResult.Error);
        }

        List<PortfolioEntry> portfolio = await GetUserPortfolioAsync(request.UserId, cancellationToken);
        if (portfolio.Count == 0)
        {
            return Result.Success(new GetMyPortfolioResponse(0, []));
        }

        Dictionary<string, decimal> prices = await GetCurrentPricesAsync(portfolio, cancellationToken);

        List<PortfolioPositionResponse> positions = BuildPositions(portfolio, prices);
        GetMyPortfolioResponse response = BuildResponse(positions);

        logger.LogDebug("Retrieved portfolio for user {UserId} with {Count} positions", request.UserId, positions.Count);

        return Result.Success(response);
    }

    private async Task<Result> EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);

        return exists
            ? Result.Success()
            : Result.Failure(UserErrors.NotFound(userId));
    }

    private async Task<List<PortfolioEntry>> GetUserPortfolioAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.PortfolioEntries
            .AsNoTracking()
            .Include(p => p.Stock)
            .Where(p => p.UserId == userId && p.Quantity > 0)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<PortfolioEntry> portfolio, CancellationToken cancellationToken)
    {
        IEnumerable<string> symbols = portfolio.Select(p => p.Stock.Symbol);

        Result<Dictionary<string, decimal>> result = await stocksApi.GetLastPricesAsync(symbols!, cancellationToken);

        return result.IsSuccess ? result.Value : [];
    }

    private static List<PortfolioPositionResponse> BuildPositions(IEnumerable<PortfolioEntry> portfolio, Dictionary<string, decimal> prices)
    {
        return portfolio.Select(p =>
        {
            string symbol = p.Stock.Symbol;
            decimal currentPrice = prices.TryGetValue(symbol, out decimal price) ? price : p.AvgPrice;
            decimal marketValue = p.Quantity * currentPrice;

            return new PortfolioPositionResponse(symbol, p.Quantity, p.AvgPrice, currentPrice, marketValue);
        }).ToList();
    }

    private static GetMyPortfolioResponse BuildResponse(List<PortfolioPositionResponse> positions)
    {
        decimal totalValue = positions.Sum(p => p.MarketValue);
        return new GetMyPortfolioResponse(totalValue, positions);
    }
}
