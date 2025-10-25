namespace Stockhub.Common.Messaging.Consumers.Configuration;

public sealed record KafkaSettings
{
    public string BootstrapServers { get; init; }
    public Dictionary<string, KafkaConsumerSettings> Consumers { get; init; } = [];
}
