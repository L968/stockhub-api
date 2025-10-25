using Microsoft.Extensions.Primitives;
using Stockhub.Modules.Orders.Application.Features.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Presentation.Orders.v1;

internal sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("orders",
            async (
                CreateOrderRequest request,
                ISender sender,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!context.Request.Headers.TryGetValue("X-User-Id", out StringValues userIdHeader)
                    || !Guid.TryParse(userIdHeader, out Guid userId))
                {
                    return Results.BadRequest(new { error = "Missing or invalid X-User-Id header." });
                }

                var command = new PlaceOrderCommand(
                    userId,
                    request.StockId,
                    request.Side,
                    request.Price,
                    request.Quantity
                );

                Result<Guid> result = await sender.Send(command, cancellationToken);

                return result.Match(
                    onSuccess: id => Results.Created($"/orders/{id}", new { id }),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Orders)
        .MapToApiVersion(1);
    }

    internal sealed record CreateOrderRequest(
        Guid StockId,
        OrderSide Side,
        decimal Price,
        int Quantity
    );
}
