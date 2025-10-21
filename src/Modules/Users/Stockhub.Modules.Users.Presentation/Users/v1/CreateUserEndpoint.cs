using Stockhub.Modules.Users.Application.Features.CreateUser;

namespace Stockhub.Modules.Users.Presentation.Users.v1;

internal sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users",
            async (
                CreateUserCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await sender.Send(command, cancellationToken);

                return result.Match(
                    onSuccess: userId => Results.Created($"/users/{userId}", new { userId }),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Users)
        .MapToApiVersion(1);
    }
}
