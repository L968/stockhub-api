namespace Stockhub.Common.Messaging.Consumers.Configuration;

public sealed record KafkaConsumerSettings
{
    public string Topic { get; init; }
    public string GroupId { get; init; }
    public string AutoOffsetReset { get; init; } = "Earliest";
    public bool EnableAutoCommit { get; init; }
}
