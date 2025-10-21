using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Features.GetStockBySymbol;

public sealed record GetStockBySymbolQuery(string Symbol) : IRequest<Result<GetStockBySymbolResponse>>;
