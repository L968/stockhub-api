using Stockhub.Common.Domain;
using Stockhub.Modules.Orders.Domain.Orders;
using Stockhub.Modules.Orders.Domain.PortfolioEntries;

namespace Stockhub.Modules.Orders.Domain.Users;

public sealed class User : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FullName { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; private set; }
    public ICollection<PortfolioEntry> PortfolioEntries { get; private set; }
}
