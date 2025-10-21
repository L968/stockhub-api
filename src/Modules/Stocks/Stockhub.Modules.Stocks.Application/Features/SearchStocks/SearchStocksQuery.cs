using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Features.SearchStocks;

public sealed record SearchStocksQuery(string SearchTerm) : IRequest<Result<List<SearchStocksResponse>>>;
