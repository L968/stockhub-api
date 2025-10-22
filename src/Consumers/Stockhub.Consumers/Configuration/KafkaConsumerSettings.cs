namespace Stockhub.Consumers.Configuration;

public sealed record KafkaConsumerSettings
{
    public required string BootstrapServers { get; init; }
    public required Dictionary<string, KafkaConsumerOptions> Consumers { get; init; } = [];
}

