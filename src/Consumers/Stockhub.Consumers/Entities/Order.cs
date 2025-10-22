using Stockhub.Common.Domain;

namespace Stockhub.Consumers.Entities;

public sealed class Order : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid StockId { get; private set; }
    public OrderSide Side { get; private set; }
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
    public int FilledQuantity { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
