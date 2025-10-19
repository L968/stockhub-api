namespace Stockhub.Modules.Users.Application.Users.GetCurrentUser;

public sealed record GetUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    DateTime CreatedAt,
    decimal CurrentBalance
);
