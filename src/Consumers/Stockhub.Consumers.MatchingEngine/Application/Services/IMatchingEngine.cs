using Stockhub.Consumers.MatchingEngine.Domain.Events.OrderPlaced;

namespace Stockhub.Consumers.MatchingEngine.Application.Services;

internal interface IMatchingEngine
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ProcessAsync(OrderPlacedEvent order, CancellationToken cancellationToken);
}
