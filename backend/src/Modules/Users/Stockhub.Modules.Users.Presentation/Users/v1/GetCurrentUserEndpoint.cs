using Microsoft.Extensions.Primitives;
using Stockhub.Modules.Users.Application.Features.GetCurrentUser;

namespace Stockhub.Modules.Users.Presentation.Users.v1;

internal sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me",
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

                var query = new GetCurrentUserQuery(userId);
                Result<GetUserResponse> result = await sender.Send(query, cancellationToken);

                return result.Match(
                    onSuccess: response => Results.Ok(response),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Users)
        .MapToApiVersion(1);
    }
}
