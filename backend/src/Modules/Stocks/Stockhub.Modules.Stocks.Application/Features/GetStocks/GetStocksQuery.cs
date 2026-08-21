using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Features.GetStocks;

public sealed record GetStocksQuery : IRequest<Result<List<GetStocksResponse>>>;
