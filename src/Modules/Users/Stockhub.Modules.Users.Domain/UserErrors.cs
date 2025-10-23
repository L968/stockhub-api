using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Users.Domain;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound(
            "User.NotFound",
            $"The user with identifier \"{userId}\" was not found."
        );

    public static Error EmailAlreadyExists =>
        Error.Conflict(
            "User.EmailAlreadyExists",
            $"Thi email is already registered."
        );
}
