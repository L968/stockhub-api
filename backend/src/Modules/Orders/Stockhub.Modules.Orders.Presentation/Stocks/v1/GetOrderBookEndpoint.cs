using Stockhub.Modules.Orders.Application.Features.Stocks.GetOrderBook;

namespace Stockhub.Modules.Orders.Presentation.Stocks.v1;

internal sealed class GetOrderBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("stocks/{symbol}/order-book",
            async (
                string symbol,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<GetOrderBookResponse> result = await sender.Send(new GetOrderBookQuery(symbol), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Stocks)
        .MapToApiVersion(1);
    }
}
