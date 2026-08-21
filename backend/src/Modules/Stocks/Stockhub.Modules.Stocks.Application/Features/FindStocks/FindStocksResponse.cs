namespace Stockhub.Modules.Stocks.Application.Features.FindStocks;

public sealed record FindStocksResponse(
    Guid Id,
    string Symbol,
    string Name
);
