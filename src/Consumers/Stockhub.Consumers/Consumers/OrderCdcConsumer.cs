using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stockhub.Consumers.Configuration;
using Stockhub.Consumers.Events;
using Stockhub.Consumers.Events.Debezium;
using Stockhub.Consumers.Events.OrderPlaced;
using Stockhub.Consumers.Matching;

namespace Stockhub.Consumers.Consumers;

internal sealed class OrderCdcConsumer(
    IServiceProvider serviceProvider,
    ILogger<OrderCdcConsumer> logger,
    KafkaSettings kafkaSettings
) : KafkaConsumerBackgroundService<DebeziumEnvelope<OrderEventPayload>>(
        serviceProvider,
        logger,
        "postgres.orders.order",
        BuildConsumerConfig(kafkaSettings.Consumers["OrderPlaced"], kafkaSettings.BootstrapServers)
    )
{
    protected override async Task HandleMessageAsync(
        DebeziumEnvelope<OrderEventPayload> envelope,
        IServiceProvider scope,
        CancellationToken cancellationToken)
    {
        if (envelope.Payload is null)
        {
            return;
        }

        DebeziumPayload<OrderEventPayload> payload = envelope.Payload;

        switch (payload.Op)
        {
            case "c":
                OrderPlacedMapper mapper = scope.GetRequiredService<OrderPlacedMapper>();
                IMatchingEngine matchingEngine = scope.GetRequiredService<IMatchingEngine>();

                OrderPlacedEvent? placedEvent = mapper.Map(payload);
                if (placedEvent is not null)
                {
                    await matchingEngine.ProcessAsync(placedEvent, cancellationToken);
                }

                break;

            default:
                logger.LogWarning("Unknown op {Op}", payload.Op);
                break;
        }
    }

    private static ConsumerConfig BuildConsumerConfig(KafkaConsumerSettings settings, string bootstrapServers)
        => new()
        {
            BootstrapServers = bootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(settings.AutoOffsetReset, ignoreCase: true),
            EnableAutoCommit = settings.EnableAutoCommit
        };
}
