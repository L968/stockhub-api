using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngineService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ProcessAsync(Order incomingOrder, CancellationToken cancellationToken);
}
