using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Stocks.GetStocks;

public sealed record GetStocksQuery : IRequest<Result<List<GetStocksResponse>>>;
