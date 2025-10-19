using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;
using Stockhub.Modules.Orders.Domain.Stocks;
using Stockhub.Modules.Orders.Domain.Trades;
using Stockhub.Modules.Orders.Domain.Users;
using Stockhub.Modules.Orders.Infrastructure.Database.Configurations;

namespace Stockhub.Modules.Orders.Infrastructure.Database;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options), IOrdersDbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<PortfolioEntry> PortfolioEntries { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Orders);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderConfiguration).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
