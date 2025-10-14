using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Domain.Products;

namespace Stockhub.Modules.Orders.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductHandler(
    IOrdersDbContext dbContext,
    ILogger<DeleteProductHandler> logger
) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? existingProduct = await dbContext.Products.FindAsync([request.Id], cancellationToken);

        if (existingProduct is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        dbContext.Products.Remove(existingProduct);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Successfully deleted Product with Id {Id}", request.Id);

        return Result.Success();
    }
}
