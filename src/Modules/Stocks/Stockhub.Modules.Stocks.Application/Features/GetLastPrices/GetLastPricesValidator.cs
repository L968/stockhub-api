namespace Stockhub.Modules.Stocks.Application.Features.GetLastPrices;

internal sealed class GetLastPricesValidator : AbstractValidator<GetLastPricesQuery>
{
    public GetLastPricesValidator()
    {
        RuleFor(x => x.Symbols)
            .NotEmpty()
            .Must(s => s.All(symbol => !string.IsNullOrWhiteSpace(symbol)))
            .WithMessage("Symbols cannot contain empty or whitespace values.");
    }
}
