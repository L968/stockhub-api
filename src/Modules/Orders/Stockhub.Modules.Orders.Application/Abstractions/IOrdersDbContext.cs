using Stockhub.Modules.Orders.Domain.Products;

namespace Stockhub.Modules.Orders.Application.Abstractions;

public interface IOrdersDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
