using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class MatchingEngineService(
    IOrderBookRepository orderBookRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    OrderValidator orderValidator,
    ILogger<MatchingEngineService> logger
) : IMatchingEngineService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IEnumerable<Order> openOrders = await orderRepository.GetAllOpenOrdersAsync(cancellationToken);

        orderBookRepository.BuildFromOrders(openOrders);

        logger.LogInformation("Matching Engine started with {Count} existing orders", openOrders.Count());
    }

    public async Task<List<Trade>> ProcessAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        Result orderValidation = await ValidateOrderAsync(incomingOrder, cancellationToken);
        if (orderValidation.IsFailure)
        {
            return [];
        }

        orderBookRepository.AddOrder(incomingOrder);

        List<Trade> executedTrades = await ExecuteOrderMatchingAsync(incomingOrder, cancellationToken);

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
            "Invalid order {OrderId}: {Errors}",
            order.Id,
            string.Join("; ", validationError.Errors.Select(e => e.Description))
        );

        order.Cancel();
        orderBookRepository.CancelOrder(order.Id);
        await orderRepository.CancelAsync(order.Id, cancellationToken);

        return Result.Failure(validationError);
    }

    private async Task<List<Trade>> ExecuteOrderMatchingAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        var executedTrades = new List<Trade>();
        OrderBook orderBook = orderBookRepository.GetOrderBookSnapshot(incomingOrder.StockId);
        int safetyLimit = orderBook.Count * 2;
        int iterationCount = 0;

        while (iterationCount++ < safetyLimit)
        {
            List<TradeProposal> proposals = orderBook.ProposeTrades(incomingOrder);

            if (!proposals.Any())
            {
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
                $"Potential infinite loop detected while matching order {incomingOrder.Id}. Iteration limit ({safetyLimit}) exceeded."
            );
        }

        return executedTrades;
    }

    private async Task<Result<Trade>> ExecuteTradeProposalAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder, User buyer, User seller) = await LoadOrdersAndUsersAsync(proposal, cancellationToken);

        Result buyValidation = await ValidateOrderAsync(buyOrder, cancellationToken);
        if (buyValidation.IsFailure)
        {
            return buyValidation;
        }

        Result sellValidation = await ValidateOrderAsync(sellOrder, cancellationToken);
        if (sellValidation.IsFailure)
        {
            return buyValidation;
        }

        var trade = new Trade(proposal, buyer, seller);

        buyOrder.Fill(trade.Quantity);
        sellOrder.Fill(trade.Quantity);

        await ApplyTradeToDatabaseAsync(trade, buyOrder, sellOrder, buyer, seller, cancellationToken);

        orderBookRepository.UpdateOrderFilledQuantity(buyOrder.Id, buyOrder.FilledQuantity);
        orderBookRepository.UpdateOrderFilledQuantity(sellOrder.Id, sellOrder.FilledQuantity);

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

    private async Task ApplyTradeToDatabaseAsync(
        Trade trade,
        Order buyOrder,
        Order sellOrder,
        User buyer,
        User seller,
        CancellationToken cancellationToken)
    {
        await orderRepository.UpdateFilledQuantityAsync(buyOrder.Id, buyOrder.FilledQuantity, cancellationToken);
        await orderRepository.UpdateFilledQuantityAsync(sellOrder.Id, sellOrder.FilledQuantity, cancellationToken);

        buyer.Debit(trade.TotalValue);
        seller.Credit(trade.TotalValue);
        await userRepository.UpdateBalanceAsync(buyer.Id, buyer.CurrentBalance, cancellationToken);
        await userRepository.UpdateBalanceAsync(seller.Id, seller.CurrentBalance, cancellationToken);

        await orderRepository.AddTradeAsync(trade, cancellationToken);
    }

    private static ValidationError ToValidationError(ValidationResult validation)
    {
        Error[] errors = validation.Errors
            .Select(f => Error.Problem(f.ErrorCode ?? f.PropertyName, f.ErrorMessage))
            .ToArray();

        return new ValidationError(errors);
    }
}
