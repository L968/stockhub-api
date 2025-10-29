using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Domain.Errors;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    IOrderBookRepository orderBookRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Order> openOrders = await orderRepository.GetAllOpenOrdersAsync(cancellationToken);

        orderBookRepository.BuildFromOrders(openOrders);

        logger.LogInformation("Matching Engine started with {Count} existing orders", orderBookRepository.TotalOrders);
    }

    public async Task<List<Trade>> ProcessAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        if (incomingOrder.Side == OrderSide.Buy && !await HasSufficientBalance(incomingOrder, cancellationToken))
        {
            logger.LogWarning("Insufficient balance for buyer {BuyerId}, cancelling order {OrderId}", incomingOrder.UserId, incomingOrder.Id);

            await orderRepository.CancelAsync(incomingOrder.Id, cancellationToken);
            return [];
        }

        OrderBook orderBook = orderBookRepository.Get(incomingOrder.StockId);
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

            await orderRepository.CancelAsync(buyOrder.Id, cancellationToken);
            orderBook.Cancel(buyOrder.Id);

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
        Order buyOrder = await orderRepository.GetAsync(proposal.BuyOrderId, cancellationToken) ?? throw new InvalidOperationException("Buy order not found");
        Order sellOrder = await orderRepository.GetAsync(proposal.SellOrderId, cancellationToken) ?? throw new InvalidOperationException("Sell order not found");
        User buyer = await userRepository.GetAsync(buyOrder.UserId, cancellationToken) ?? throw new InvalidOperationException("Buyer not found");
        User seller = await userRepository.GetAsync(sellOrder.UserId, cancellationToken) ?? throw new InvalidOperationException("Seller not found");

        return (buyOrder, sellOrder, buyer, seller);
    }

    private async Task<bool> HasSufficientBalance(Order order, CancellationToken cancellationToken)
    {
        decimal requiredAmount = order.Price * order.Quantity;
        return await userRepository.HasSufficientBalanceAsync(order.UserId, requiredAmount, cancellationToken);
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
        await orderRepository.UpdateFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity, cancellationToken);
        await orderRepository.UpdateFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity, cancellationToken);

        buyer.CurrentBalance -= trade.Price * trade.Quantity;
        seller.CurrentBalance += trade.Price * trade.Quantity;
        await userRepository.UpdateBalanceAsync(buyer.Id, buyer.CurrentBalance, cancellationToken);
        await userRepository.UpdateBalanceAsync(seller.Id, seller.CurrentBalance, cancellationToken);

        await orderRepository.AddTradeAsync(trade, cancellationToken);
    }
}
