using Microsoft.Extensions.Primitives;
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
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!context.Request.Headers.TryGetValue("X-User-Id", out StringValues userIdHeader)
                    || !Guid.TryParse(userIdHeader, out Guid userId))
                {
                    return Results.BadRequest(new { error = "Missing or invalid X-User-Id header." });
                }

                var query = new GetMyTradesQuery(
                    userId,
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
