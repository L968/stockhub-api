using Stockhub.Modules.Stocks.Application.Stocks.CreateStock;

namespace Stockhub.Modules.Stocks.Presentation.Stocks.v1;

internal sealed class CreateStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("stocks",
            async (
                CreateStockCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await sender.Send(command, cancellationToken);

                return result.Match(
                    onSuccess: stockId => Results.Created($"/stocks/{stockId}", new { stockId }),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
