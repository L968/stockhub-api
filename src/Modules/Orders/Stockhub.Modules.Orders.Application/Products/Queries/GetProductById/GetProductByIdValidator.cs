namespace Stockhub.Modules.Orders.Application.Products.Queries.GetProductById;

internal sealed class GetProductByIdValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty();
    }
}
