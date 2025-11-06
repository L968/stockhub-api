using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngineService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task EnqueueOrderAsync(Order incomingOrder, CancellationToken cancellationToken);
    Task<List<Trade>> ProcessOrderBookAsync(Guid stockId, CancellationToken cancellationToken);
}
