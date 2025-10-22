namespace Stockhub.Consumers.Configuration;

public sealed record KafkaConsumerOptions
{
    public required string Topic { get; init; }
    public required string GroupId { get; init; }
    public string AutoOffsetReset { get; init; } = "Earliest";
    public bool EnableAutoCommit { get; init; }
}
