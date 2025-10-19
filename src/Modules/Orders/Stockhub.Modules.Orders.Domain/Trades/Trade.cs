using Stockhub.Common.Domain;

namespace Stockhub.Modules.Orders.Domain.Trades;

public sealed class Trade : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid BuyOrderId { get; private set; }
    public Guid SellOrderId { get; private set; }
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
