using Stockhub.Modules.Users.Application.Products.Queries.GetProductById;

namespace Stockhub.Modules.Users.Presentation.Products.v1;

internal sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products/{id:Guid}",
            async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetProductByIdQuery(id);
                Result<GetProductByIdResponse> result = await sender.Send(query, cancellationToken);

                return result.Match(
                    onSuccess: response => Results.Ok(response),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Products)
        .MapToApiVersion(1);
    }
}
