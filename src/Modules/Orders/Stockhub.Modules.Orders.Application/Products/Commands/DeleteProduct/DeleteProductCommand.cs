using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
