using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Modules.Stocks.Application.Abstractions;
using Stockhub.Modules.Stocks.Domain;
using Stockhub.Modules.Stocks.Infrastructure.Database.Configurations;

namespace Stockhub.Modules.Stocks.Infrastructure.Database;

public sealed class StocksDbContext(DbContextOptions<StocksDbContext> options) : DbContext(options), IStocksDbContext
{
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockSnapshot> StockSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Stocks);
        modelBuilder.ApplyConfiguration(new StockConfiguration());
        modelBuilder.ApplyConfiguration(new StockSnapshotConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
