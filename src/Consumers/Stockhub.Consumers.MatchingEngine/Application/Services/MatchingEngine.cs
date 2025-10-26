using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
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

    public async Task ProcessAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        OrderBook orderBook = GetOrCreateOrderBook(incomingOrder.StockId);
        orderBook.Add(incomingOrder);

        bool continueProcessing = true;
        while (continueProcessing)
        {
            List<TradeProposal> proposals = orderBook.ProposeTrades(incomingOrder);
            continueProcessing = false;

            foreach (TradeProposal proposal in proposals)
            {
                bool tradeExecuted = await ProcessTradeProposalAsync(proposal, orderBook, cancellationToken);

                if (!tradeExecuted)
                {
                    continueProcessing = true;
                    break;
                }
            }
        }

        if (orderBook.IsEmpty)
        {
            _orderBooks.Remove(incomingOrder.StockId);
        }
    }

    private async Task<bool> ProcessTradeProposalAsync(TradeProposal proposal, OrderBook orderBook, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder, User buyer, User seller) = await LoadOrdersAndUsersAsync(proposal, cancellationToken);

        decimal totalValue = proposal.Price * proposal.Quantity;
        if (buyer.CurrentBalance < totalValue)
        {
            logger.LogWarning("Insufficient balance for buyer {BuyerId}, cancelling order {OrderId}", buyer.Id, buyOrder.Id);

            buyOrder.Cancel();
            await ordersDbContext.SaveChangesAsync(cancellationToken);

            orderBook.Remove(buyOrder.Id);

            return false;
        }

        Trade trade = CreateTrade(proposal, buyer, seller);

        await ApplyTradeToDatabaseAsync(trade, buyOrder, sellOrder, buyer, seller, cancellationToken);

        orderBook.CommitTrade(trade);

        logger.LogInformation(
            "Trade executed: {StockId} | Buy {BuyOrderId} ↔ Sell {SellOrderId} @ {Price} x {Quantity}",
            trade.StockId, trade.BuyOrderId, trade.SellOrderId, trade.Price, trade.Quantity
        );

        return true;
    }

    private async Task<(Order buyOrder, Order sellOrder, User buyer, User seller)> LoadOrdersAndUsersAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        Order buyOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == proposal.BuyOrderId, cancellationToken);
        Order sellOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == proposal.SellOrderId, cancellationToken);
        User buyer = await usersDbContext.Users.FirstAsync(u => u.Id == buyOrder.UserId, cancellationToken);
        User seller = await usersDbContext.Users.FirstAsync(u => u.Id == sellOrder.UserId, cancellationToken);

        return (buyOrder, sellOrder, buyer, seller);
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

            orderBook.Add(new Order
            {
                Id = order.Id,
                UserId = order.UserId,
                StockId = order.StockId,
                Side = order.Side,
                Price = order.Price,
                Quantity = order.Quantity,
                FilledQuantity = order.FilledQuantity,
                IsCancelled = order.IsCancelled,
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

    private static Trade CreateTrade(TradeProposal proposal, User buyer, User seller)
        => new(
            stockId: proposal.StockId,
            buyerId: buyer.Id,
            sellerId: seller.Id,
            buyOrderId: proposal.BuyOrderId,
            sellOrderId: proposal.SellOrderId,
            price: proposal.Price,
            quantity: proposal.Quantity
        );

    private async Task ApplyTradeToDatabaseAsync(
        Trade trade,
        Order buyOrder,
        Order sellOrder,
        User buyer,
        User seller,
        CancellationToken cancellationToken)
    {
        buyOrder.Fill(trade.Quantity);
        sellOrder.Fill(trade.Quantity);

        buyer.CurrentBalance -= trade.Price * trade.Quantity;
        seller.CurrentBalance += trade.Price * trade.Quantity;

        ordersDbContext.Trades.Add(trade);

        await ordersDbContext.SaveChangesAsync(cancellationToken);
        await usersDbContext.SaveChangesAsync(cancellationToken);
    }
}
