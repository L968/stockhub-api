using Stockhub.Consumers.Entities;

namespace Stockhub.Consumers.Events.OrderPlaced;

internal sealed record OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid StockId { get; set; }
    public OrderSide Side { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
