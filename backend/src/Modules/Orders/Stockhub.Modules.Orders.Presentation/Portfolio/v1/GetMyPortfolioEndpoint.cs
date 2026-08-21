using Microsoft.Extensions.Primitives;
using Stockhub.Modules.Orders.Application.Features.Portfolio.GetMyPortfolio;

namespace Stockhub.Modules.Orders.Presentation.Portfolio.v1;

internal sealed class GetMyPortfolioEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("portfolio/me",
            async (
                ISender sender,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!context.Request.Headers.TryGetValue("X-User-Id", out StringValues userIdHeader)
                    || !Guid.TryParse(userIdHeader, out Guid userId))
                {
                    return Results.BadRequest(new { error = "Missing or invalid X-User-Id header." });
                }

                Result<GetMyPortfolioResponse> result = await sender.Send(new GetMyPortfolioQuery(userId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Portfolio)
        .MapToApiVersion(1);
    }
}
