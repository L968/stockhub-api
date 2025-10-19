namespace Stockhub.Modules.Users.Application.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<GetUserResponse>>;
