namespace Stockhub.Modules.Orders.Application.Orders.GetMyOrders;

internal sealed class GetMyOrdersValidator : AbstractValidator<GetMyOrdersQuery>
{
    public GetMyOrdersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
