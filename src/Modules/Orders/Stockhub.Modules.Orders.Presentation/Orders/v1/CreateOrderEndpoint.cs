using Stockhub.Modules.Orders.Application.Orders.PlaceOrder;
using Stockhub.Modules.Orders.Domain.Orders;

namespace Stockhub.Modules.Orders.Presentation.Orders.v1;

internal sealed class CreateOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("orders",
            async (
                CreateOrderRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new PlaceOrderCommand(
                    Guid.CreateVersion7(),
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
