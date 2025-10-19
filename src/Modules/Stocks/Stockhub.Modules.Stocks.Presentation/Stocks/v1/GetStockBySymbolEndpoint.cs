using Stockhub.Modules.Stocks.Application.Stocks.GetStockBySymbol;

namespace Stockhub.Modules.Stocks.Presentation.Stocks.v1;

internal sealed class GetStockBySymbolEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stocks/{symbol:string}",
            async (
                string symbol,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<GetStockBySymbolResponse> result = await sender.Send(new GetStockBySymbolQuery(symbol), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
