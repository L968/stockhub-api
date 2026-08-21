namespace Stockhub.Modules.Orders.Application.Features.Stocks.GetOrderBook;

public sealed record GetOrderBookResponse(
    IEnumerable<OrderBookEntry> Bids,
    IEnumerable<OrderBookEntry> Asks,
    DateTime UpdatedAtUtc
);

public sealed record OrderBookEntry(
    decimal Price,
    int Quantity,
    int OrderCount
);
