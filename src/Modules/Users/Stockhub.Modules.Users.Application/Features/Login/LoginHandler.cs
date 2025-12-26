using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain;

namespace Stockhub.Modules.Users.Application.Features.Login;

internal sealed class LoginHandler(
    IUsersDbContext dbContext,
    ILogger<LoginHandler> logger
) : IRequestHandler<LoginCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.InvalidCredentials);
        }

        logger.LogDebug("User {Email} logged in", request.Email);

        return user.Id;
    }
}
