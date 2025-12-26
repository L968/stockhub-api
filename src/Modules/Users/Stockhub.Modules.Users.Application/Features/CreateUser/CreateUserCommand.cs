namespace Stockhub.Modules.Users.Application.Features.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string FullName
) : IRequest<Result<Guid>>;
