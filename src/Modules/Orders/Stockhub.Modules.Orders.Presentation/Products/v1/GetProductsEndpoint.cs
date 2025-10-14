using Microsoft.AspNetCore.Mvc;
using Stockhub.Common.Application;
using Stockhub.Modules.Orders.Application.Products.Queries.GetProducts;

namespace Stockhub.Modules.Orders.Presentation.Products.v1;

internal sealed class GetProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products", async (
                ISender sender,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10) =>
            {
                var query = new GetProductsQuery(page, pageSize);
                PaginatedList<GetProductsResponse> response = await sender.Send(query, cancellationToken);

                return Results.Ok(response);
            })
            .WithTags(Tags.Products)
            .MapToApiVersion(1);
    }
}
