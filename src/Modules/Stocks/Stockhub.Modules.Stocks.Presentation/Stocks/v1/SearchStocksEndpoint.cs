using Microsoft.AspNetCore.Mvc;
using Stockhub.Modules.Stocks.Application.Features.SearchStocks;

namespace Stockhub.Modules.Stocks.Presentation.Stocks.v1;

internal sealed class SearchStocksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stocks",
            async (
                [FromQuery] string search,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new SearchStocksQuery(search);
                Result<List<SearchStocksResponse>> result = await sender.Send(query, cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
