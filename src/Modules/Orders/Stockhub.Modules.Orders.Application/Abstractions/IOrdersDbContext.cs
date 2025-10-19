using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;
using Stockhub.Modules.Orders.Domain.Stocks;
using Stockhub.Modules.Orders.Domain.Trades;
using Stockhub.Modules.Orders.Domain.Users;

namespace Stockhub.Modules.Orders.Application.Abstractions;

public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<PortfolioEntry> PortfolioEntries { get; }
    DbSet<Stock> Stocks { get; }
    DbSet<Trade> Trades { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
