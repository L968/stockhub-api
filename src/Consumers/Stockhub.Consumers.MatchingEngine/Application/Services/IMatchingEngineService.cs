using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngineService
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task EnqueueOrderAsync(Order incomingOrder, CancellationToken cancellationToken);
    Task<List<Trade>> MatchPendingOrdersAsync(Guid stockId, CancellationToken cancellationToken);
}
