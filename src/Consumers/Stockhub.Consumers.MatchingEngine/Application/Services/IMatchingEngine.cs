using Stockhub.Consumers.MatchingEngine.Domain.Entities;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngine
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ProcessAsync(Order order, CancellationToken cancellationToken);
}
