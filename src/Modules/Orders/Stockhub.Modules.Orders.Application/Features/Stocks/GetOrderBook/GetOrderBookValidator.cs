namespace Stockhub.Modules.Orders.Application.Features.Stocks.GetOrderBook;

internal sealed class GetOrderBookValidator : AbstractValidator<GetOrderBookQuery>
{
    public GetOrderBookValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(100);
    }
}
