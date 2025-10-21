namespace Stockhub.Modules.Stocks.Application.Features.SearchStocks;

public sealed record SearchStocksResponse(
    Guid Id,
    string Symbol,
    string Name
);
