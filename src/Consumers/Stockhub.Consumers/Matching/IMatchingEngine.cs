using Stockhub.Consumers.Events.OrderPlaced;

namespace Stockhub.Consumers.Matching;

internal interface IMatchingEngine
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ProcessAsync(OrderPlacedEvent order, CancellationToken cancellationToken);
}
