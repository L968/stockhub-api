using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Products;

namespace Stockhub.Modules.Orders.Application.Products.Commands.UpdateProduct;

internal sealed class UpdateProductHandler(
    IOrdersDbContext dbContext,
    ILogger<UpdateProductHandler> logger
) : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.FindAsync([request.Id], cancellationToken);

        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        product.Update(
            request.Name,
            request.Price
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Successfully updated {@Product}", product);

        return Result.Success();
    }
}
