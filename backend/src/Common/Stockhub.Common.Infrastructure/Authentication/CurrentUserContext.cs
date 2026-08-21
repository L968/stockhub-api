using System.Security.Claims;
using Stockhub.Common.Application.Authentication;

namespace Stockhub.Common.Infrastructure.Authentication;

public sealed class CurrentUserContext : ICurrentUserContext
{
    public Guid UserId { get; }

    public CurrentUserContext(ClaimsPrincipal user)
    {
        string? userIdClaim = user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            throw new InvalidOperationException("Invalid user ID in claims.");
        }

        UserId = userId;
    }
}
