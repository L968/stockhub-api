namespace Stockhub.Modules.Stocks.Application.Products.Commands.CreateProduct;

public sealed record CreateProductResponse(
    Guid Id,
    string Name,
    decimal Price
);
