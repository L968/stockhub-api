using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Errors;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    OrdersDbContext ordersDbContext,
    UsersDbContext usersDbContext,
    IOrderBookRepository orderBookRepository,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        List<Order> openOrders = await ordersDbContext.Orders
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PartiallyFilled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        orderBookRepository.BuildFromOrders(openOrders);

        logger.LogInformation("Matching Engine started with {Count} existing orders", orderBookRepository.TotalOrders);
    }

    public async Task<List<Trade>> ProcessAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        OrderBook orderBook = orderBookRepository.Get(incomingOrder.StockId);

        if (incomingOrder.Side == OrderSide.Buy && !await HasSufficientBalance(incomingOrder, cancellationToken))
        {
            logger.LogWarning("Insufficient balance for buyer {BuyerId}, cancelling order {OrderId}", incomingOrder.UserId, incomingOrder.Id);

            await CancelOrder(orderBook, incomingOrder.Id, cancellationToken);
            return [];
        }

        orderBook.Add(incomingOrder);

        var executedTrades = new List<Trade>();
        while (true)
        {
            List<TradeProposal> proposals = orderBook.ProposeTrades(incomingOrder);

            if (!proposals.Any())
            {
                break;
            }

            foreach (TradeProposal proposal in proposals)
            {
                Result<Trade> result = await ProcessTradeProposalAsync(proposal, orderBook, cancellationToken);

                if (result.IsFailure)
                {
                    break;
                }

                executedTrades.Add(result.Value);
            }
        }

        if (orderBook.IsEmpty)
        {
            orderBookRepository.Remove(incomingOrder.StockId);
        }

        return executedTrades;
    }

    private async Task<Result<Trade>> ProcessTradeProposalAsync(TradeProposal proposal, OrderBook orderBook, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder, User buyer, User seller) = await LoadOrdersAndUsersAsync(proposal, cancellationToken);

        decimal totalValue = proposal.Price * proposal.Quantity;
        if (buyer.CurrentBalance < totalValue)
        {
            logger.LogWarning("Insufficient balance for buyer {BuyerId}, cancelling order {OrderId}", buyer.Id, buyOrder.Id);

            await CancelOrder(orderBook, buyOrder.Id, cancellationToken);

            return Result.Failure(OrderErrors.InsufficientBalance);
        }

        var trade = new Trade(proposal, buyer, seller);

        await ApplyTradeToDatabaseAsync(trade, buyOrder, sellOrder, buyer, seller, cancellationToken);

        orderBook.CommitTrade(trade);

        logger.LogInformation(
            "Trade executed: {StockId} | Buy {BuyOrderId} ↔ Sell {SellOrderId} @ {Price} x {Quantity}",
            trade.StockId, trade.BuyOrderId, trade.SellOrderId, trade.Price, trade.Quantity
        );

        return trade;
    }

    private async Task<(Order buyOrder, Order sellOrder, User buyer, User seller)> LoadOrdersAndUsersAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        Order buyOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == proposal.BuyOrderId, cancellationToken);
        Order sellOrder = await ordersDbContext.Orders.FirstAsync(o => o.Id == proposal.SellOrderId, cancellationToken);
        User buyer = await usersDbContext.Users.FirstAsync(u => u.Id == buyOrder.UserId, cancellationToken);
        User seller = await usersDbContext.Users.FirstAsync(u => u.Id == sellOrder.UserId, cancellationToken);

        return (buyOrder, sellOrder, buyer, seller);
    }

    private async Task<bool> HasSufficientBalance(Order order, CancellationToken cancellationToken)
    {
        decimal totalValue = order.Price * order.Quantity;

        return await usersDbContext.Users
            .Where(u => u.Id == order.UserId)
            .Select(u => u.CurrentBalance >= totalValue)
            .FirstAsync(cancellationToken);
    }

    private async Task CancelOrder(OrderBook orderBook, Guid orderId, CancellationToken cancellationToken)
    {
        await ordersDbContext.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.IsCancelled, true), cancellationToken);

        await ordersDbContext.SaveChangesAsync(cancellationToken);

        orderBook.Cancel(orderId);
    }

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
