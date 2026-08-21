using Stockhub.Consumers.MatchingEngine.Domain.Enums;

namespace Stockhub.Consumers.MatchingEngine.Domain.Entities;

internal sealed class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StockId { get; set; }
    public OrderSide Side { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public OrderStatus Status => IsCancelled switch
    {
        true => OrderStatus.Cancelled,
        _ when FilledQuantity == 0 => OrderStatus.Pending,
        _ when FilledQuantity < Quantity => OrderStatus.PartiallyFilled,
        _ => OrderStatus.Filled
    };

    public void Fill(int quantity)
    {
        if (IsCancelled)
        {
            throw new InvalidOperationException("Cannot fill a cancelled order.");
        }

        FilledQuantity = Math.Min(FilledQuantity + quantity, Quantity);
    }

    public void Cancel()
    {
        if (IsCancelled)
        {
            return;
        }

        if (Status == OrderStatus.Filled)
        {
            throw new InvalidOperationException("Cannot cancel a fully filled order.");
        }

        IsCancelled = true;
    }
}
