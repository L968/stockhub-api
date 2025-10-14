using Stockhub.Modules.Orders.Application.Products.Commands.UpdateProduct;

namespace Stockhub.Modules.Orders.Presentation.Products.v1;

internal sealed class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("products/{id:Guid}",
            async (
                Guid id,
                UpdateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateProductCommand(
                    id,
                    request.Name,
                    request.Price
                );

                Result result = await sender.Send(command, cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .WithTags(Tags.Products)
        .MapToApiVersion(1);
    }

    internal sealed record UpdateProductRequest(string Name, decimal Price);
}
