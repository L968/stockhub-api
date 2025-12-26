using Microsoft.Extensions.Primitives;
using Stockhub.Common.Application;
using Stockhub.Modules.Orders.Application.Features.Orders.GetMyOrders;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Presentation.Orders.v1;

internal sealed class GetMyOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("orders/me",
            async (
                int page,
                int pageSize,
                DateTime? startDate,
                DateTime? endDate,
                OrderStatus? status,
                ISender sender,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!context.Request.Headers.TryGetValue("X-User-Id", out StringValues userIdHeader)
                    || !Guid.TryParse(userIdHeader, out Guid userId))
                {
                    return Results.BadRequest(new { error = "Missing or invalid X-User-Id header." });
                }

                var query = new GetMyOrdersQuery(
                    userId,
                    page,
                    pageSize,
                    startDate,
                    endDate,
                    status
                );

                Result<PaginatedList<GetMyOrdersResponse>> result = await sender.Send(query, cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Orders)
        .MapToApiVersion(1);
    }
}
