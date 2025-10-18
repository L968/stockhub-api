using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
