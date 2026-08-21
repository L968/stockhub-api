namespace Stockhub.Modules.Users.Application.Features.Login;

public sealed record LoginCommand(
    string Email
) : IRequest<Result<Guid>>;
