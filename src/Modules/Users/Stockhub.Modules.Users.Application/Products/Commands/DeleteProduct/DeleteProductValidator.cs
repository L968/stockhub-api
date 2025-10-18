namespace Stockhub.Modules.Users.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty();
    }
}
