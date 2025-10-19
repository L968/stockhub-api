namespace Stockhub.Modules.Stocks.Application.Stocks.SearchStocks;

public sealed record SearchStocksResponse(
    Guid Id,
    string Symbol,
    string Name
);
