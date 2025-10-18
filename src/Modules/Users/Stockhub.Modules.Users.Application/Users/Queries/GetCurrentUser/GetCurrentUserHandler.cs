using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain.Users;

namespace Stockhub.Modules.Users.Application.Users.Queries.GetCurrentUser;

internal sealed class GetCurrentUserHandler(
    IUsersDbContext dbContext,
    ILogger<GetCurrentUserHandler> logger
) : IRequestHandler<GetCurrentUserQuery, Result<GetUserResponse>>
{
    public async Task<Result<GetUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<GetUserResponse>(UserErrors.NotFound(request.UserId));
        }

        var response = new GetUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAtUtc,
            user.CurrentBalance
        );

        logger.LogDebug("Fetched user {@User}", user);

        return Result.Success(response);
    }
}
