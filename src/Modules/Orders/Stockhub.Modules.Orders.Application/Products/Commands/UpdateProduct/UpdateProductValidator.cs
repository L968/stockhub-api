namespace Stockhub.Modules.Orders.Application.Products.Commands.UpdateProduct;

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(p => p.Price)
            .GreaterThan(0);
    }
}
