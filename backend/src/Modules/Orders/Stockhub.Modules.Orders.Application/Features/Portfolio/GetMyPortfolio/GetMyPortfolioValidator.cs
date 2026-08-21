namespace Stockhub.Modules.Orders.Application.Features.Portfolio.GetMyPortfolio;

internal sealed class GetMyPortfolioValidator : AbstractValidator<GetMyPortfolioQuery>
{
    public GetMyPortfolioValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
