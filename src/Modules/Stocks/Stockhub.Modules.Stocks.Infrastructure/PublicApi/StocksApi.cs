using MediatR;
using Stockhub.Common.Domain.Results;
using Stockhub.Modules.Stocks.Application.Features.GetLastPrices;
using Stockhub.Modules.Stocks.PublicApi;

namespace Stockhub.Modules.Stocks.Infrastructure.PublicApi;

internal sealed class StocksApi(ISender sender) : IStocksApi
{
    public async Task<Result<Dictionary<string, decimal>>> GetLastPricesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        var query = new GetLastPricesQuery(symbols);
        Result<GetLastPricesResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return null;
        }

        return result.Value.Prices;
    }
}
