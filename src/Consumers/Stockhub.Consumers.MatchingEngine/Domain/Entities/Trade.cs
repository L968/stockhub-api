using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;

namespace Stockhub.Consumers.MatchingEngine.Domain.Entities;

internal sealed class Trade
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

    public decimal TotalValue => Price * Quantity;

    public Trade(Guid stockId, Guid buyerId, Guid sellerId, Guid buyOrderId, Guid sellOrderId, decimal price, int quantity)
    {
        Id = Guid.CreateVersion7();
        StockId = stockId;
        BuyerId = buyerId;
        SellerId = sellerId;
        BuyOrderId = buyOrderId;
        SellOrderId = sellOrderId;
        Price = price;
        Quantity = quantity;
        ExecutedAt = DateTime.UtcNow;
    }

    public Trade(TradeProposal proposal, User buyer, User seller)
        : this(
            stockId: proposal.StockId,
            buyerId: buyer.Id,
            sellerId: seller.Id,
            buyOrderId: proposal.BuyOrderId,
            sellOrderId: proposal.SellOrderId,
            price: proposal.Price,
            quantity: proposal.Quantity
        )
    {
    }
}
