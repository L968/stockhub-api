namespace Stockhub.Consumers.Configuration;

internal sealed record KafkaSettings
{
    public string BootstrapServers { get; init; }
    public Dictionary<string, KafkaConsumerSettings> Consumers { get; init; } = [];
}
