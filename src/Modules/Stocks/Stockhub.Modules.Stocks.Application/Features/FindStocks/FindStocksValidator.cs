using Stockhub.Modules.Stocks.Application.Features.SearchStocks;

namespace Stockhub.Modules.Stocks.Application.Features.FindStocks;

internal sealed class FindStocksValidator : AbstractValidator<FindStocksQuery>
{
    public FindStocksValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50);
    }
}
