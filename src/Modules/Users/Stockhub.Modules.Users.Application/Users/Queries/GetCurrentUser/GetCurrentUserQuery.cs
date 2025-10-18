namespace Stockhub.Modules.Users.Application.Users.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<GetUserResponse>>;
