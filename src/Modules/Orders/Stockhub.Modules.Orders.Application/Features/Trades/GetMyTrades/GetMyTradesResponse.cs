namespace Stockhub.Modules.Orders.Application.Features.Trades.GetMyTrades;

public sealed record GetMyTradesResponse(
    Guid Id,
    string Symbol,
    string Side,
    decimal Price,
    int Quantity,
    Guid OrderId,
    DateTime ExecutedAt
);
