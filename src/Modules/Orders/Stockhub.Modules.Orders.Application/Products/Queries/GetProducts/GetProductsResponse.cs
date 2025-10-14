namespace Stockhub.Modules.Orders.Application.Products.Queries.GetProducts;

public sealed record GetProductsResponse(
    Guid Id,
    string Name,
    decimal Price
);
