using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Stocks.SearchStocks;

public sealed record SearchStocksQuery(string SearchTerm) : IRequest<Result<List<SearchStocksResponse>>>;
