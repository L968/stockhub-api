using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.Stocks;

namespace Stockhub.Modules.Orders.Application.Features.Stocks.GetOrderBook;

internal sealed class GetOrderBookHandler(
    IOrdersDbContext dbContext,
    ILogger<GetOrderBookHandler> logger
) : IRequestHandler<GetOrderBookQuery, Result<GetOrderBookResponse>>
{
    public async Task<Result<GetOrderBookResponse>> Handle(GetOrderBookQuery request, CancellationToken cancellationToken)
    {
        List<Order> stockOrders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending && o.Stock.Symbol == request.Symbol)
            .ToListAsync(cancellationToken);

        if (!stockOrders.Any())
        {
            return Result.Failure(StockErrors.SymbolNotFound(request.Symbol));
        }

        var bids = stockOrders
            .Where(o => o.Side == OrderSide.Buy)
            .GroupBy(o => o.Price)
            .Select(g => new OrderBookEntry(
                g.Key,
                g.Sum(x => x.Quantity - x.FilledQuantity),
                g.Count()
            ))
            .OrderByDescending(e => e.Price)
            .ToList();

        var asks = stockOrders
            .Where(o => o.Side == OrderSide.Sell)
            .GroupBy(o => o.Price)
            .Select(g => new OrderBookEntry(
                g.Key,
                g.Sum(x => x.Quantity - x.FilledQuantity),
                g.Count()
            ))
            .OrderBy(e => e.Price)
            .ToList();

        var response = new GetOrderBookResponse(
            bids,
            asks,
            DateTime.UtcNow
        );

        logger.LogDebug("Retrieved order book for stock {Symbol}", request.Symbol);

        return Result.Success(response);
    }
}
