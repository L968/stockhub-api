namespace Stockhub.Modules.Stocks.Application.Features.SearchStocks;

public sealed record FindStocksResponse(
    Guid Id,
    string Symbol,
    string Name
);
