using Stockhub.Common.Application;
using Stockhub.Modules.Users.Application.Abstractions;

namespace Stockhub.Modules.Users.Application.Products.Queries.GetProducts;

internal sealed class GetProductsHandler(
    IUsersDbContext dbContext,
    ILogger<GetProductsHandler> logger
) : IRequestHandler<GetProductsQuery, PaginatedList<GetProductsResponse>>
{
    public async Task<PaginatedList<GetProductsResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        int totalItems = await dbContext.Products.CountAsync(cancellationToken);

        List<GetProductsResponse> products = await dbContext.Products
            .AsNoTracking()
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new GetProductsResponse(p.Id, p.Name, p.Price))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} products", products.Count);

        return new PaginatedList<GetProductsResponse>(
            request.Page,
            request.PageSize,
            totalItems,
            products
        );
    }
}
