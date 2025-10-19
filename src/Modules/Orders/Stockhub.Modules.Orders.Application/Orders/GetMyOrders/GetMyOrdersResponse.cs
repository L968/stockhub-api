namespace Stockhub.Modules.Orders.Application.Orders.GetMyOrders;

public sealed record GetMyOrdersResponse(
    Guid Id,
    string Side,
    decimal Price,
    int Quantity,
    int FilledQuantity,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    OrderStockResponse Stock
);

public sealed record OrderStockResponse(
    Guid Id,
    string Symbol,
    string Name
);
