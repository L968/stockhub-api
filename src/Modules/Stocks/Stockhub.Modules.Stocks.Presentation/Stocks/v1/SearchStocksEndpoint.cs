using Microsoft.AspNetCore.Mvc;
using Stockhub.Modules.Stocks.Application.Features.SearchStocks;

namespace Stockhub.Modules.Stocks.Presentation.Stocks.v1;

internal sealed class FindStocksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stocks/find",
            async (
                [FromQuery(Name = "query")] string query,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var request = new FindStocksQuery(query);
                Result<List<FindStocksResponse>> result = await sender.Send(request, cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
