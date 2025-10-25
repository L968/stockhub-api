namespace Stockhub.Consumers.Events.Debezium;

internal sealed class DebeziumEnvelope<TValue>
{
    public DebeziumPayload<TValue>? Payload { get; set; }
}
