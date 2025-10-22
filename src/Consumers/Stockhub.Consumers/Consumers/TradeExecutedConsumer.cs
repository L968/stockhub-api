using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stockhub.Consumers.Configuration;
using Stockhub.Consumers.Events;
using Stockhub.Consumers.Handlers;

namespace Stockhub.Consumers.Consumers;

internal sealed class TradeExecutedConsumer(
    IServiceProvider serviceProvider,
    ILogger<TradeExecutedConsumer> logger,
    IOptions<KafkaConsumerSettings> kafkaOptions
) : KafkaConsumerBackgroundService<TradeExecutedEvent>(
    serviceProvider,
    logger,
    topic: kafkaOptions.Value.Consumers["TradeExecuted"].Topic,
    config: BuildConfig(kafkaOptions.Value, "TradeExecuted"))
{
    protected override async Task HandleMessageAsync(TradeExecutedEvent message, IServiceProvider scope, CancellationToken cancellationToken)
    {
        TradeExecutedEventHandler handler = scope.GetRequiredService<TradeExecutedEventHandler>();
        await handler.HandleAsync(message, cancellationToken);
    }

    private static ConsumerConfig BuildConfig(KafkaConsumerSettings settings, string consumerKey)
    {
        KafkaConsumerOptions consumer = settings.Consumers[consumerKey];

        return new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = consumer.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(consumer.AutoOffsetReset, ignoreCase: true),
            EnableAutoCommit = consumer.EnableAutoCommit
        };
    }
}
