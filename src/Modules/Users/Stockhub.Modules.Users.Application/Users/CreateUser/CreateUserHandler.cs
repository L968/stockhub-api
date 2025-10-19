using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain;

namespace Stockhub.Modules.Users.Application.Users.CreateUser;

internal sealed class CreateUserHandler(
    IUsersDbContext dbContext,
    ILogger<CreateUserHandler> logger
) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        User user = new(
            email: request.Email,
            fullName: request.FullName,
            currentBalance: 1000m
        );

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Created new user {@User}", user);

        return Result.Success(user.Id);
    }
}
