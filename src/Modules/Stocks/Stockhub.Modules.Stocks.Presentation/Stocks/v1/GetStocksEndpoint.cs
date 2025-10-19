using Stockhub.Modules.Stocks.Application.Stocks.GetStocks;

namespace Stockhub.Modules.Stocks.Presentation.Stocks.v1;

internal sealed class GetStocksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stocks",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<List<GetStocksResponse>> result = await sender.Send(new GetStocksQuery(), cancellationToken);

                return result.Match(
                    Results.Ok,
                    ApiResults.Problem
                );
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
