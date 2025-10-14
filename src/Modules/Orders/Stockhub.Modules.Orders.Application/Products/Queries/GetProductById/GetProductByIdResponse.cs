namespace Stockhub.Modules.Orders.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdResponse(
    Guid Id,
    string Name,
    decimal Price
);
