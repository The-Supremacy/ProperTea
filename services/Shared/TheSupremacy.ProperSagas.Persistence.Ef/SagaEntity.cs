using TheSupremacy.ProperSagas.Domain;

namespace TheSupremacy.ProperSagas.Persistence.Ef;

public class SagaEntity
{
    public Guid Id { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string? DisplayName { get; set; } = string.Empty;
    public SagaStatus Status { get; set; } = SagaStatus.Pending;
    public int Version { get; set; } = 0;
    public Guid? LockToken { get; set; }
    public DateTime? LockedAt { get; set; }
    public string SagaData { get; set; } = "{}";
    public string Steps { get; set; } = "[]";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public bool IsCancellationRequested { get; set; } = false;
    public DateTime? CancellationRequestedAt { get; set; }
    public DateTime? TimeoutDeadline { get; set; }
    public DateTime? ScheduledFor { get; set; }
}