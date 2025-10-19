using Stockhub.Modules.Users.Application.Users.GetCurrentUser;

namespace Stockhub.Modules.Users.Presentation.Users.v1;

internal sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCurrentUserQuery(Guid.CreateVersion7());
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
