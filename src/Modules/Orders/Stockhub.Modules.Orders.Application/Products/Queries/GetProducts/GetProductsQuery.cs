using Stockhub.Common.Application;

namespace Stockhub.Modules.Orders.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int Page, int PageSize) : IRequest<PaginatedList<GetProductsResponse>>;
