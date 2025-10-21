using Stockhub.Common.Application;
using Stockhub.Modules.Orders.Application.Features.Trades.GetMyTrades;

namespace Stockhub.Modules.Orders.Presentation.Trades.v1;

internal sealed class GetMyTradesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("trades/me",
            async (
                int page,
                int pageSize,
                DateTime? startDate,
                DateTime? endDate,
                string? symbol,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMyTradesQuery(
                    Guid.CreateVersion7(),
                    page,
                    pageSize,
                    startDate,
                    endDate,
                    symbol
                );

                Result<PaginatedList<GetMyTradesResponse>> result = await sender.Send(query, cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Trades)
        .MapToApiVersion(1);
    }
}
