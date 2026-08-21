using Stockhub.Common.Domain;
using Stockhub.Modules.Orders.Domain.Stocks;

namespace Stockhub.Modules.Orders.Domain.PortfolioEntries;

public sealed class PortfolioEntry : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid StockId { get; private set; }
    public int Quantity { get; private set; }
    public decimal AvgPrice { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Stock Stock { get; private set; }
}
