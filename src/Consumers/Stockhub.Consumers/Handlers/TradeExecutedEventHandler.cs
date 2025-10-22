using Microsoft.Extensions.Logging;
using Stockhub.Consumers.Events;

namespace Stockhub.Consumers.Handlers;

internal sealed class TradeExecutedEventHandler(
    ILogger<TradeExecutedEventHandler> logger
)
{
    public async Task HandleAsync(TradeExecutedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing TradeExecutedEvent for {Symbol} ({Quantity} @ {Price})",
            @event.Symbol, @event.Quantity, @event.Price
        );

        await Task.Delay(10, cancellationToken);
    }
}
