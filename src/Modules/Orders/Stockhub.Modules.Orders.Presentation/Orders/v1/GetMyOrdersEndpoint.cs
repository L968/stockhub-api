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
                CancellationToken cancellationToken) =>
            {
                var query = new GetMyOrdersQuery(
                    Guid.CreateVersion7(),
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
