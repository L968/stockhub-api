using Stockhub.Common.Application;
using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Orders.Application.Features.Trades.GetMyTrades;

public sealed record GetMyTradesQuery(
    Guid UserId,
    int Page,
    int PageSize,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Symbol
) : IRequest<Result<PaginatedList<GetMyTradesResponse>>>;
