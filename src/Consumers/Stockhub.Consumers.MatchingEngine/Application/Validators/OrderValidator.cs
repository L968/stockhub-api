using FluentValidation;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Enums;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;

namespace Stockhub.Consumers.MatchingEngine.Application.Validators;

internal sealed class OrderValidator : AbstractValidator<Order>
{
    private readonly IUserRepository _userRepository;

    public OrderValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(o => o.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero");

        RuleFor(o => o.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(o => o.IsCancelled)
            .Equal(false)
            .WithMessage("Cannot process cancelled order");

        RuleFor(o => o)
            .MustAsync(HaveSufficientBalance)
            .WithMessage(o => "The user does not have enough balance to place this buy order.");
    }

    private async Task<bool> HaveSufficientBalance(Order order, CancellationToken ct)
    {
        if (order.Side != OrderSide.Buy)
        {
            return true;
        }

        decimal requiredAmount = order.Price * order.Quantity;
        return await _userRepository.HasSufficientBalanceAsync(order.UserId, requiredAmount, ct);
    }
}
