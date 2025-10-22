using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Features.SearchStocks;

public sealed record FindStocksQuery(string Query) : IRequest<Result<List<FindStocksResponse>>>;
