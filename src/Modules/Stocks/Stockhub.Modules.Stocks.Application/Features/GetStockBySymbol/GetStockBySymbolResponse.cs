namespace Stockhub.Modules.Stocks.Application.Features.GetStockBySymbol;

public sealed record GetStockBySymbolResponse(
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
