using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Queues;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    IOrderBookRepository orderBookRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IDirtyQueue dirtyQueue,
    OrderValidator orderValidator,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Order> openOrders = await orderRepository.GetAllOpenOrdersAsync(cancellationToken);

        orderBookRepository.BuildFromOrders(openOrders);

        foreach (Guid stockId in openOrders.Select(o => o.StockId).Distinct())
        {
            dirtyQueue.Enqueue(stockId);
        }

        logger.LogInformation("Matching Engine started with {Count} existing orders", openOrders.Count());
    }

    public async Task EnqueueOrderAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        Result validation = await ValidateOrderAsync(incomingOrder, cancellationToken);
        if (validation.IsFailure)
        {
            await CommitCancelOrder(incomingOrder, cancellationToken);
            return;
        }

        if (orderBookRepository.ContainsOrder(incomingOrder.Id))
        {
            return;
        }

        orderBookRepository.AddOrder(incomingOrder);
        dirtyQueue.Enqueue(incomingOrder.StockId);
    }

    public async Task<List<Trade>> ProcessOrderBookAsync(Guid stockId, CancellationToken cancellationToken)
    {
        OrderBook orderBook = orderBookRepository.GetOrderBookSnapshot(stockId);

        if (orderBook.Count == 0)
        {
            dirtyQueue.MarkProcessed(stockId);
            return [];
        }

        var executedTrades = new List<Trade>();
        int safetyLimit = orderBook.Count * 2;
        int iterationCount = 0;

        while (iterationCount++ < safetyLimit)
        {
            List<TradeProposal> proposals = orderBook.ProposeAllPossibleTrades();

            if (!proposals.Any())
            {
                dirtyQueue.MarkProcessed(stockId);
                break;
            }

            foreach (TradeProposal proposal in proposals)
            {
                Result<Trade> result = await ExecuteTradeProposalAsync(proposal, cancellationToken);

                if (result.IsFailure)
                {
                    break;
                }

                executedTrades.Add(result.Value);
            }
        }

        if (iterationCount >= safetyLimit)
        {
            throw new InvalidOperationException(
                $"Potential infinite loop detected while matching stock {stockId}. Iteration limit ({safetyLimit}) exceeded."
            );
        }

        return executedTrades;
    }

    private async Task<Result> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
    {
        ValidationResult validation = await orderValidator.ValidateAsync(order, cancellationToken);

        if (validation.IsValid)
        {
            return Result.Success();
        }

        ValidationError validationError = ToValidationError(validation);

        logger.LogWarning(
            "Invalid order {OrderId} (User {UserId}, Stock {StockId}): {Errors}",
            order.Id, order.UserId, order.StockId,
            string.Join("; ", validationError.Errors.Select(e => e.Description))
        );

        return Result.Failure(validationError);
    }

    private async Task<Result<Trade>> ExecuteTradeProposalAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder, User buyer, User seller) = await LoadOrdersAndUsersAsync(proposal, cancellationToken);

        Result buyValidation = await ValidateOrderAsync(buyOrder, cancellationToken);
        if (buyValidation.IsFailure)
        {
            await CommitCancelOrder(buyOrder, cancellationToken);
            return buyValidation;
        }

        Result sellValidation = await ValidateOrderAsync(sellOrder, cancellationToken);
        if (sellValidation.IsFailure)
        {
            await CommitCancelOrder(sellOrder, cancellationToken);
            return sellValidation;
        }

        var trade = new Trade(proposal, buyer, seller);

        await CommitTrade(trade, buyOrder, sellOrder, buyer, seller, cancellationToken);

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

    private async Task CommitTrade(
        Trade trade,
        Order buyOrder,
        Order sellOrder,
        User buyer,
        User seller,
        CancellationToken cancellationToken)
    {
        buyOrder.Fill(trade.Quantity);
        sellOrder.Fill(trade.Quantity);
        await orderRepository.UpdateFilledQuantityAsync(buyOrder.Id, buyOrder.FilledQuantity, cancellationToken);
        await orderRepository.UpdateFilledQuantityAsync(sellOrder.Id, sellOrder.FilledQuantity, cancellationToken);

        buyer.Debit(trade.TotalValue);
        seller.Credit(trade.TotalValue);
        await userRepository.UpdateBalanceAsync(buyer.Id, buyer.CurrentBalance, cancellationToken);
        await userRepository.UpdateBalanceAsync(seller.Id, seller.CurrentBalance, cancellationToken);

        await orderRepository.AddTradeAsync(trade, cancellationToken);

        orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
        orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);
    }

    private async Task CommitCancelOrder(Order order, CancellationToken cancellationToken)
    {
        await orderRepository.CancelAsync(order.Id, cancellationToken);
        orderBookRepository.CancelOrder(order.Id);
    }

    private static ValidationError ToValidationError(ValidationResult validation)
    {
        Error[] errors = validation.Errors
            .Select(f => Error.Problem(f.ErrorCode ?? f.PropertyName, f.ErrorMessage))
            .ToArray();

        return new ValidationError(errors);
    }
}
