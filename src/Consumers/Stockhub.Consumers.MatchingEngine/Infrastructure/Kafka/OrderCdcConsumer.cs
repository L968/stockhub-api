using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Stockhub.Common.Messaging.Consumers.Configuration;
using Stockhub.Common.Messaging.Consumers.Debezium;
using Stockhub.Common.Messaging.Consumers.Kafka;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Domain.Entities;
using Stockhub.Consumers.MatchingEngine.Domain.Events;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka.Mappers;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka;

internal sealed class OrderCdcConsumer(
    IMatchingEngineService matchingEngine,
    OrderPlacedMapper mapper,
    ILogger<OrderCdcConsumer> logger,
    KafkaSettings kafkaSettings
) : KafkaConsumerBackgroundService<DebeziumEnvelope<OrderEventPayload>>(
        logger,
        kafkaSettings.Consumers["OrderPlaced"].Topic,
        BuildConsumerConfig(kafkaSettings.Consumers["OrderPlaced"], kafkaSettings.BootstrapServers)
    )
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await matchingEngine.StartAsync(stoppingToken);
        await base.ExecuteAsync(stoppingToken);
    }

    protected override async Task HandleMessageAsync(
        DebeziumEnvelope<OrderEventPayload> envelope,
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
                Order? placedEvent = mapper.Map(payload);

                if (placedEvent is not null)
                {
                    await matchingEngine.EnqueueOrderAsync(placedEvent, cancellationToken);
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
