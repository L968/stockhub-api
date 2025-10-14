using Stockhub.Modules.Orders.Application.Products.Commands.DeleteProduct;

namespace Stockhub.Modules.Orders.Presentation.Products.v1;

internal sealed class DeleteProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("products/{id:Guid}",
            async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new DeleteProductCommand(id), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .WithTags(Tags.Products)
        .MapToApiVersion(1);
    }
}
