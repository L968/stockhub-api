using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Stocks.GetStockBySymbol;

public sealed record GetStockBySymbolQuery(string Symbol) : IRequest<Result<GetStockBySymbolResponse>>;
