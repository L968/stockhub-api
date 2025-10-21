namespace Stockhub.Modules.Orders.Application.Features.Trades.GetMyTrades;

internal sealed class GetMyTradesValidator : AbstractValidator<GetMyTradesQuery>
{
    public GetMyTradesValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.Symbol)
            .MinimumLength(1)
            .MaximumLength(100);
    }
}
