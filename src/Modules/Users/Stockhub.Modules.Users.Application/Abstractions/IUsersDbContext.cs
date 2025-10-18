using Stockhub.Modules.Users.Domain.Users;

namespace Stockhub.Modules.Users.Application.Abstractions;

public interface IUsersDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
