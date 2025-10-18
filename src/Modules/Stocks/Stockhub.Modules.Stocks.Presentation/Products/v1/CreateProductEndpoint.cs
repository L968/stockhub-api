using Stockhub.Modules.Stocks.Application.Products.Commands.CreateProduct;
using Stockhub.Modules.Stocks.Presentation;

namespace Stockhub.Modules.Stocks.Presentation.Products.v1;

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
