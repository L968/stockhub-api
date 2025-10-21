using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Domain.Stocks;

public static class StockErrors
{
    public static Error SymbolNotFound(string symbol) =>
        Error.NotFound(
            "Stock.SymbolNotFound",
            $"The symbol with identifier \"{symbol}\" was not found."
        );
}
