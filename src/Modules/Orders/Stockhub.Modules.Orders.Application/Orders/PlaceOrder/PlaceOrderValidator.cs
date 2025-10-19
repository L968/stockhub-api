namespace Stockhub.Modules.Orders.Application.Orders.PlaceOrder;

internal sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(o => o.UserId)
            .NotEmpty();

        RuleFor(o => o.StockId)
            .NotEmpty();

        RuleFor(o => o.Price)
            .GreaterThan(0);

        RuleFor(o => o.Quantity)
            .GreaterThan(0);
    }
}
