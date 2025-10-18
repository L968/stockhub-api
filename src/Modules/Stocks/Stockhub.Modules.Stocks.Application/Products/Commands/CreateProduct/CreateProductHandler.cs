using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Abstractions;
using Stockhub.Modules.Stocks.Domain.Products;

namespace Stockhub.Modules.Stocks.Application.Products.Commands.CreateProduct;

internal sealed class CreateProductHandler(
    IStocksDbContext dbContext,
    ILogger<CreateProductHandler> logger
) : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        bool existingProduct = await dbContext.Products.AnyAsync(p => p.Name == request.Name, cancellationToken);
        if (existingProduct)
        {
            return Result.Failure(ProductErrors.ProductAlreadyExists(request.Name));
        }

        var product = new Product(
            request.Name,
            request.Price
        );

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Successfully created {@Product}", product);

        return Result.Success(
            new CreateProductResponse(
                product.Id,
                product.Name,
                product.Price
            )
        );
    }
}
