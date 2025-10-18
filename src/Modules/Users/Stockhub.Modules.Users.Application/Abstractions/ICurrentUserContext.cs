namespace Stockhub.Modules.Users.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }
}
