namespace Stockhub.Modules.Users.Application.Features.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<GetUserResponse>>;
