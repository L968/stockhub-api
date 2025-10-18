using Stockhub.Modules.Users.Domain.Products;

namespace Stockhub.Modules.Users.Application.Abstractions;

public interface IUsersDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
