using Stockhub.Modules.Stocks.Domain.Products;

namespace Stockhub.Modules.Stocks.Application.Abstractions;

public interface IStocksDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
