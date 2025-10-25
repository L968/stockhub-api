namespace Stockhub.Consumers.Events.Debezium;

internal sealed class DebeziumPayload<TValue>
{
    public TValue? Before { get; set; }
    public TValue? After { get; set; }
    public string Op { get; set; } = default!;
}
