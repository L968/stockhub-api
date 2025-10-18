using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Domain.Products;

namespace Stockhub.Modules.Users.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductHandler(
    IUsersDbContext dbContext,
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
