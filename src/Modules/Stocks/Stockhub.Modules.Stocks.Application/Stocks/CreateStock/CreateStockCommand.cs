using Stockhub.Common.Domain.Results;

namespace Stockhub.Modules.Stocks.Application.Stocks.CreateStock;

public sealed record CreateStockCommand(
    string Symbol,
    string Name,
    string Sector
) : IRequest<Result<Guid>>;
