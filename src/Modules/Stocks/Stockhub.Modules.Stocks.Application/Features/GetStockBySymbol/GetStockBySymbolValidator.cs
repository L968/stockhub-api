namespace Stockhub.Modules.Stocks.Application.Features.GetStockBySymbol;

internal sealed class GetStockBySymbolValidator : AbstractValidator<GetStockBySymbolQuery>
{
    public GetStockBySymbolValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(100);
    }
}
