namespace Stockhub.Modules.Users.Application.Features.GetCurrentUser;

public sealed record GetUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    DateTime CreatedAt,
    decimal CurrentBalance
);
