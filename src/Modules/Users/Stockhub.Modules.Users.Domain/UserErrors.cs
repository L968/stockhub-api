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
            "This email is already registered."
        );

    public static Error InvalidCredentials =>
        Error.NotFound(
            "User.InvalidCredentials",
            "Invalid credentials."
        );
}
