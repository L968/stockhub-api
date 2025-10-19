using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Domain;

public static class StockErrors
{
    public static Error StockAlreadyExists(string symbol) =>
        Error.Conflict(
            "Stock.AlreadyExists",
            $"A stock with symbol \"{symbol}\" already exists."
        );

    public static Error NotFound(Guid stockId) =>
        Error.NotFound(
            "Stock.NotFound",
            $"The stock with identifier \"{stockId}\" was not found."
        );

    public static Error SymbolNotFound(string symbol) =>
        Error.NotFound(
            "Stock.SymbolNotFound",
            $"The stock with symbol \"{symbol}\" was not found."
        );
}
