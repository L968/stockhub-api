using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Features.GetLastPrices;

public sealed record GetLastPricesQuery(IEnumerable<string> Symbols) : IRequest<Result<GetLastPricesResponse>>;
