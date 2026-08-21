using Stockhub.Common.Domain;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;

namespace Stockhub.Modules.Orders.Domain.Stocks;

public sealed class Stock : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; }
    public string Name { get; private set; }
    public string Sector { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; private set; }
    public ICollection<PortfolioEntry> PortfolioEntries { get; private set; }
}
