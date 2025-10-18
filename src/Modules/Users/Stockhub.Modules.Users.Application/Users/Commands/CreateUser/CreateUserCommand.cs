namespace Stockhub.Modules.Users.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<Result<Guid>>;
