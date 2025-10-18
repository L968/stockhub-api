using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<GetProductByIdResponse>>;
