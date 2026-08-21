namespace Stockhub.Modules.Orders.Application.Features.Portfolio.GetMyPortfolio;

public sealed record GetMyPortfolioResponse(
    decimal TotalValue,
    IEnumerable<PortfolioPositionResponse> Positions
);

public sealed record PortfolioPositionResponse(
    string Symbol,
    int Quantity,
    decimal AvgPrice,
    decimal CurrentPrice,
    decimal MarketValue
);

