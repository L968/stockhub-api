using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<Result<CreateProductResponse>>;
