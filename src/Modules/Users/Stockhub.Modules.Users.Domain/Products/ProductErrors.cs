using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Users.Domain.Products;

public static class ProductErrors
{
    public static Error ProductAlreadyExists(string productName) =>
        Error.Conflict(
            "Product.AlreadyExists",
            $"A product with name \"{productName}\" already exists."
        );

    public static Error NotFound(Guid productId) =>
        Error.NotFound(
            "Product.NotFound",
            $"The product with identifier \"{productId}\" was not found."
        );
}
