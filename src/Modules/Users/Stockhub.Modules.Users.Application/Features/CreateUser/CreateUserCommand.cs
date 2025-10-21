namespace Stockhub.Modules.Users.Application.Features.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<Result<Guid>>;
