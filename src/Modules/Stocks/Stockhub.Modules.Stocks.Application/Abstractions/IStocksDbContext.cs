using Stockhub.Modules.Stocks.Domain;

namespace Stockhub.Modules.Stocks.Application.Abstractions;

public interface IStocksDbContext
{
    DbSet<Stock> Stocks { get; }
    DbSet<StockSnapshot> StockSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
