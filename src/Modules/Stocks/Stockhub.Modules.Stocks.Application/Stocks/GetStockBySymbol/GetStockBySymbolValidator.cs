namespace Stockhub.Modules.Stocks.Application.Stocks.GetStockBySymbol;

internal sealed class GetStockBySymbolValidator : AbstractValidator<GetStockBySymbolQuery>
{
    public GetStockBySymbolValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(100);
    }
}
