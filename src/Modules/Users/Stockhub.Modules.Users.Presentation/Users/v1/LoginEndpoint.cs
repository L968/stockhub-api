using Stockhub.Modules.Users.Application.Features.Login;

namespace Stockhub.Modules.Users.Presentation.Users.v1;

internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login",
            async (
                LoginCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await sender.Send(command, cancellationToken);

                return result.Match(
                    onSuccess: userId => Results.Ok(new { userId }),
                    onFailure: ApiResults.Problem
                );
            })
        .WithTags(Tags.Users)
        .MapToApiVersion(1);
    }
}
