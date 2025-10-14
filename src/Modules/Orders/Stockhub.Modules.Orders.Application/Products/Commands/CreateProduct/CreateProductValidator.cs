namespace Stockhub.Modules.Orders.Application.Products.Commands.CreateProduct;

internal sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(p => p.Price)
            .GreaterThan(0);
    }
}
