using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<GetProductByIdResponse>>;
