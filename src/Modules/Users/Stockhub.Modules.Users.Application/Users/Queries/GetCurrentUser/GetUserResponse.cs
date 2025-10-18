namespace Stockhub.Modules.Users.Application.Users.Queries.GetCurrentUser;

public sealed record GetUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    DateTime CreatedAt,
    decimal CurrentBalance
);
