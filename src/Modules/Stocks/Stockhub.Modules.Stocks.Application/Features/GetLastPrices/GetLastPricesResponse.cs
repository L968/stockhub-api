namespace Stockhub.Modules.Stocks.Application.Features.GetLastPrices;

public sealed record GetLastPricesResponse(Dictionary<string, decimal> Prices);
