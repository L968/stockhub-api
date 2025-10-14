namespace Stockhub.Common.Infrastructure.Outbox;

public sealed record OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Payload { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
