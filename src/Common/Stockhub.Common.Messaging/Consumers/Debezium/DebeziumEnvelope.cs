namespace Stockhub.Common.Messaging.Consumers.Debezium;

public sealed class DebeziumEnvelope<TValue>
{
    public DebeziumPayload<TValue>? Payload { get; set; }
}
