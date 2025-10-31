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

        logger.LogInformation("Matching Engine started with {Count} existing orders", orderBookRepository.TotalOrders);
    }

    public async Task<List<Trade>> ProcessAsync(Order incomingOrder, CancellationToken cancellationToken)
    {
        OrderBook orderBook = orderBookRepository.Get(incomingOrder.StockId);

        Result orderValidation = await ValidateOrderAsync(incomingOrder, orderBook, cancellationToken);
        if (orderValidation.IsFailure)
        {
            return [];
        }

        orderBook.Add(incomingOrder);

        List<Trade> executedTrades = await ExecuteOrderMatchingAsync(incomingOrder, orderBook, cancellationToken);

        if (orderBook.IsEmpty)
        {
            orderBookRepository.Remove(incomingOrder.StockId);
        }

        return executedTrades;
    }


    private async Task<Result> ValidateOrderAsync(Order order, OrderBook orderBook, CancellationToken cancellationToken)
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

        await orderRepository.CancelAsync(order.Id, cancellationToken);
        orderBook.Cancel(order.Id);

        return Result.Failure(validationError);
    }

    private async Task<List<Trade>> ExecuteOrderMatchingAsync(Order incomingOrder, OrderBook orderBook, CancellationToken cancellationToken)
    {
        var executedTrades = new List<Trade>();
        int safetyLimit = orderBook.TotalOrders * 2;
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
                Result<Trade> result = await ExecuteTradeProposalAsync(proposal, orderBook, cancellationToken);

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

    private async Task<Result<Trade>> ExecuteTradeProposalAsync(TradeProposal proposal, OrderBook orderBook, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder, User buyer, User seller) = await LoadOrdersAndUsersAsync(proposal, cancellationToken);

        Result buyValidation = await ValidateOrderAsync(buyOrder, orderBook, cancellationToken);
        if (buyValidation.IsFailure)
        {
            return buyValidation;
        }

        Result sellValidation = await ValidateOrderAsync(sellOrder, orderBook,cancellationToken);
        if (sellValidation.IsFailure)
        {
            return buyValidation;
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
