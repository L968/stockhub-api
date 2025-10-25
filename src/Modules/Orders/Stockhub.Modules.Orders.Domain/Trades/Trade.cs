using Stockhub.Modules.Orders.Domain.Stocks;

namespace Stockhub.Modules.Orders.Domain.Trades;

public sealed class Trade
{
    public Guid Id { get; private set; }
    public Guid StockId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid BuyOrderId { get; private set; }
    public Guid SellOrderId { get; private set; }
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    public Stock Stock { get; private set; } = default!;
}
