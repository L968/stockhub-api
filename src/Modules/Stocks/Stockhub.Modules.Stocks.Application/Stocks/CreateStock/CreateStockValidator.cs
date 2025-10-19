namespace Stockhub.Modules.Stocks.Application.Stocks.CreateStock;

internal sealed class CreateStockValidator : AbstractValidator<CreateStockCommand>
{
    public CreateStockValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(16);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(128);

        RuleFor(x => x.Sector)
            .NotEmpty()
            .MaximumLength(64);
    }
}
