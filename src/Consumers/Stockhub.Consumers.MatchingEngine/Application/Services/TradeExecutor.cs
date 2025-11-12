using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.ValueObjects;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal sealed class TradeExecutor(
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IOrderBookRepository orderBookRepository,
    OrderValidator orderValidator,
    ILogger<TradeExecutor> logger
) : ITradeExecutor
{
    public async Task<Result<Trade>> ExecuteAsync(TradeProposal proposal, CancellationToken cancellationToken)
    {
        (Order buyOrder, Order sellOrder) = await LoadOrdersAsync(proposal.BuyOrderId, proposal.SellOrderId, cancellationToken);

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

        (User buyer, User seller) = await LoadUsersAsync(buyOrder.UserId, sellOrder.UserId, cancellationToken);

        var trade = new Trade(proposal, buyer, seller);

        await PersistExecutedTradeAsync(trade, buyOrder, sellOrder, buyer, seller, cancellationToken);

        logger.LogInformation(
            "Trade executed: {StockId} | Buy {BuyOrderId} ↔ Sell {SellOrderId} @ {Price} x {Quantity}",
            trade.StockId, trade.BuyOrderId, trade.SellOrderId, trade.Price, trade.Quantity
        );

        return trade;
    }

    private async Task<Result> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
    {
        ValidationResult validation = await orderValidator.ValidateAsync(order, cancellationToken);
        if (validation.IsValid)
        {
            return Result.Success();
        }

        var validationError = new ValidationError(
            validation.Errors.Select(f => Error.Problem(f.ErrorCode ?? f.PropertyName, f.ErrorMessage)).ToArray()
        );

        logger.LogWarning(
            "Invalid order {OrderId} (User {UserId}, Stock {StockId}): {Errors}",
            order.Id, order.UserId, order.StockId,
            string.Join("; ", validationError.Errors.Select(e => e.Description))
        );

        return Result.Failure(validationError);
    }

    private async Task<(Order buyOrder, Order sellOrder)> LoadOrdersAsync(Guid buyOrderId, Guid sellOrderId, CancellationToken cancellationToken)
    {
        Order buyOrder = await orderRepository.GetAsync(buyOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Buy order not found");

        Order sellOrder = await orderRepository.GetAsync(sellOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Sell order not found");

        return (buyOrder, sellOrder);
    }

    private async Task<(User buyer, User seller)> LoadUsersAsync(Guid buyerId, Guid sellerId, CancellationToken cancellationToken)
    {
        User buyer = await userRepository.GetAsync(buyerId, cancellationToken)
            ?? throw new InvalidOperationException("Buyer not found");

        User seller = await userRepository.GetAsync(sellerId, cancellationToken)
            ?? throw new InvalidOperationException("Seller not found");

        return (buyer, seller);
    }

    private async Task PersistExecutedTradeAsync(
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
}
