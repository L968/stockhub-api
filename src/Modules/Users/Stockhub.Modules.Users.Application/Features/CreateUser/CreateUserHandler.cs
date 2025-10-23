using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain;

namespace Stockhub.Modules.Users.Application.Features.CreateUser;

internal sealed class CreateUserHandler(
    IUsersDbContext dbContext,
    ILogger<CreateUserHandler> logger
) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        bool emailExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            return Result.Failure(UserErrors.EmailAlreadyExists);
        }

        User user = new(
            email: request.Email,
            fullName: request.FullName,
            currentBalance: 1000m
        );

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Created new user {@User}", user);

        return user.Id;
    }
}
