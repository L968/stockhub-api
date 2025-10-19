using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound(
            "User.NotFound",
            $"The user with identifier \"{userId}\" was not found."
        );
}
