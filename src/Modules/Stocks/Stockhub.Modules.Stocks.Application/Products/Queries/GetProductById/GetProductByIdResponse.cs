namespace Stockhub.Modules.Stocks.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdResponse(
    Guid Id,
    string Name,
    decimal Price
);
