namespace Stockhub.Modules.Stocks.Application.Features.GetStocks;

public sealed record GetStocksResponse(
    Guid Id,
    string Symbol,
    string Name,
    decimal LastPrice,
    decimal ChangePercent,
    decimal MinPrice,
    decimal MaxPrice,
    long Volume,
    DateTime UpdatedAtUtc
);
