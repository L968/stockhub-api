namespace Stockhub.Common.Messaging;

public sealed class RabbitMqStreamOptions
{
    public const string SectionName = "RabbitMqStreams";
    public const string OrderStream = "order-placed";
    public const string MatchingConsumerGroup = "matching-engine";

    public int Port { get; init; } = 5552;
    public int Partitions { get; init; } = 16;
}
