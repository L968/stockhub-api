using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Events.OrderPlaced;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngine(
    OrdersDbContext ordersDbContext,
    UsersDbContext usersDbContext,
    ILogger<MatchingEngine> logger
) : IMatchingEngine
{
    private readonly Dictionary<Guid, OrderBook> _orderBooks = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await BuildAsync(cancellationToken);
        int totalOrders = _orderBooks.Sum(o => o.Value.TotalOrders);
        logger.LogInformation("Matching Engine started with {Count} existing orders", totalOrders);
    }

    public async Task ProcessAsync(OrderPlacedEvent order, CancellationToken cancellationToken)
    {
        OrderBook orderBook = GetOrCreateOrderBook(order.StockId);
        orderBook.Add(order);

        IEnumerable<Trade> trades = orderBook.Match(order);

        foreach (Trade trade in trades)
        {
            logger.LogInformation(
                "Trade executed: {StockId} | Buy {BuyOrderId} ↔ Sell {SellOrderId} @ {Price} x {Quantity}",
                trade.StockId, trade.BuyOrderId, trade.SellOrderId, trade.Price, trade.Quantity
            );

            await ApplyTradeEffectsAsync(trade, cancellationToken);
        }

        await ordersDbContext.SaveChangesAsync(cancellationToken);
        await usersDbContext.SaveChangesAsync(cancellationToken);

        if (orderBook.IsEmpty)
        {
            _orderBooks.Remove(order.StockId);
        }
    }

    public async Task ApplyTradeEffectsAsync(Trade trade, CancellationToken cancellationToken)
    {
        ordersDbContext.Trades.Add(trade);

        Order buyOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == trade.BuyOrderId, cancellationToken);
        Order sellOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == trade.SellOrderId, cancellationToken);

        User buyer = await usersDbContext.Users.FirstAsync(u => u.Id == buyOrder.UserId, cancellationToken);
        User seller = await usersDbContext.Users.FirstAsync(u => u.Id == sellOrder.UserId, cancellationToken);

        decimal totalValue = trade.Price * trade.Quantity;

        if (buyer.CurrentBalance < totalValue)
        {
            logger.LogWarning("Insufficient balance for buyer {BuyerId}, trade {TradeId} skipped", buyer.Id, trade.Id);
            return;
        }

        buyOrder.FilledQuantity += trade.Quantity;
        sellOrder.FilledQuantity += trade.Quantity;

        buyOrder.Status = buyOrder.FilledQuantity >= buyOrder.Quantity
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

        sellOrder.Status = sellOrder.FilledQuantity >= sellOrder.Quantity
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

        buyer.CurrentBalance -= totalValue;
        seller.CurrentBalance += totalValue;
    }

    private async Task BuildAsync(CancellationToken cancellationToken)
    {
        List<Order> openOrders = await ordersDbContext.Orders
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PartiallyFilled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (Order order in openOrders)
        {
            OrderBook orderBook = GetOrCreateOrderBook(order.StockId);

            orderBook.Add(new OrderPlacedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                StockId = order.StockId,
                Side = order.Side,
                Price = order.Price,
                Quantity = order.Quantity,
                FilledQuantity = order.FilledQuantity,
                Status = order.Status,
                CreatedAtUtc = order.CreatedAtUtc,
                UpdatedAtUtc = order.UpdatedAtUtc
            });
        }

        logger.LogInformation("OrderBooks built for {Count} stocks", _orderBooks.Count);
    }

    private OrderBook GetOrCreateOrderBook(Guid stockId)
    {
        if (!_orderBooks.TryGetValue(stockId, out OrderBook? orderBook))
        {
            orderBook = new OrderBook(stockId);
            _orderBooks[stockId] = orderBook;
        }

        return orderBook;
    }
}
