using Stockhub.Modules.Orders.Application.Products.Commands.CreateProduct;

namespace Stockhub.Modules.Orders.Presentation.Products.v1;

internal sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("products",
            async (
                CreateProductCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<CreateProductResponse> result = await sender.Send(command, cancellationToken);

                return result.Match(
                    onSuccess: response => Results.Created($"/products/{response.Id}", response),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Products)
        .MapToApiVersion(1);
    }
}
