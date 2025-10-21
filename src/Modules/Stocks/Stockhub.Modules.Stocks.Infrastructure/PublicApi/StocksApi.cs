using MediatR;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Features.GetLastPrices;
using Stockhub.Modules.Stocks.PublicApi;

namespace Stockhub.Modules.Stocks.Infrastructure.PublicApi;

internal sealed class StocksApi(
    ISender sender,
    ILogger<StocksApi> logger
) : IStocksApi
{
    public async Task<Result<Dictionary<string, decimal>>> GetLastPricesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLastPricesQuery(symbols);
        Result<GetLastPricesResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to retrieve stock prices for symbols {Symbols}: {Error}",
                string.Join(", ", symbols), result.Error.Description);

            return result.ToResult();
        }

        return result.Value.Prices;
    }
}
