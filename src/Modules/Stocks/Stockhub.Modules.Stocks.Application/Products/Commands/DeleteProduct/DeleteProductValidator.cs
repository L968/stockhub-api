namespace Stockhub.Modules.Stocks.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty();
    }
}
