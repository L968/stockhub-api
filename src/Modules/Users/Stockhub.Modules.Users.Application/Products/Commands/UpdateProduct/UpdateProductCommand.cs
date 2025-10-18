using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Users.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price
) : IRequest<Result>;
