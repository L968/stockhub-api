using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.PublicApi;

public interface IStocksApi
{
    Task<Result<Dictionary<string, decimal>>> GetLastPricesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
}
