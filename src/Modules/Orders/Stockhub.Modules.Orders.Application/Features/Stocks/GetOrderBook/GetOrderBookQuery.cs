using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Application.Features.Stocks.GetOrderBook;

public sealed record GetOrderBookQuery(
    string Symbol
) : IRequest<Result<GetOrderBookResponse>>;
