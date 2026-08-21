using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngineService
{
    Task<IReadOnlyList<Trade>> ProcessOrderAsync(
        string partition,
        Order incomingOrder,
        CancellationToken cancellationToken);
}
