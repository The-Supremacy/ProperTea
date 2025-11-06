namespace ProperTea.ProperIntegrationEvents.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public OutboxMessageStatus Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; } = null!;
}

public enum OutboxMessageStatus
{
    Pending,
    Published,
    Failed
}