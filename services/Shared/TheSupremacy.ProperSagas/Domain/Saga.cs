using System.Text.Json;

namespace TheSupremacy.ProperSagas.Domain;

public class Saga
{
    internal Saga()
    {
    }

    public Guid Id { get; internal set; } = Guid.NewGuid();
    public string SagaType { get; internal set; } = string.Empty;
    public string? DisplayName { get; internal set; } = string.Empty;
    public SagaStatus Status { get; internal set; } = SagaStatus.Pending;
    public int Version { get; internal set; }
    public Guid? LockToken { get; internal set; }
    public DateTime? LockedAt { get; internal set; }
    public List<SagaStep> Steps { get; internal set; } = [];
    public string? ErrorMessage { get; internal set; }
    public DateTime CreatedAt { get; internal set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; internal set; }
    public string SagaData { get; internal set; } = "{}";
    public string? CorrelationId { get; internal set; }
    public string? TraceId { get; internal set; }
    public string? IdempotencyKey { get; internal set; }
    public bool IsCancellationRequested { get; internal set; }
    public DateTime? CancellationRequestedAt { get; internal set; }
    public TimeSpan? Timeout { get; internal set; }
    public DateTime? TimeoutDeadline { get; internal set; }
    public DateTime? ScheduledFor { get; set; }

    public bool TryAcquireLock(Guid workerToken, TimeSpan lockTimeout)
    {
        if (LockToken.HasValue && LockedAt.HasValue)
        {
            var lockAge = DateTime.UtcNow - LockedAt.Value;
            if (lockAge < lockTimeout)
                return false;
        }

        LockToken = workerToken;
        LockedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void ReleaseLock(Guid workerToken)
    {
        if (LockToken == workerToken)
        {
            LockToken = null;
            LockedAt = null;
        }
    }

    public void SetTimeout(TimeSpan timeout)
    {
        Timeout = timeout;
        TimeoutDeadline = DateTime.UtcNow.Add(timeout);
    }

    public bool IsTimedOut()
    {
        return TimeoutDeadline.HasValue && DateTime.UtcNow > TimeoutDeadline.Value;
    }

    public void RequestCancellation()
    {
        IsCancellationRequested = true;
        CancellationRequestedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasReachedPointOfNoReturn()
    {
        return Steps.Any(s => s is { Type: SagaStepType.PointOfNoReturn, Status: SagaStepStatus.Completed });
    }

    public void SetData<T>(string key, T value)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(SagaData)
                   ?? new Dictionary<string, object>();

        data[key] = value!;
        SagaData = JsonSerializer.Serialize(data);
        UpdatedAt = DateTime.UtcNow;
    }

    public T? GetData<T>(string key)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(SagaData);
        if (data == null || !data.TryGetValue(key, out var value))
            return default;

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json);
    }

    public void MarkAsRunning()
    {
        Status = SagaStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        Status = SagaStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = SagaStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailedAfterPonr(string errorMessage)
    {
        Status = SagaStatus.FailedAfterPonr;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensating()
    {
        Status = SagaStatus.Compensating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensated()
    {
        Status = SagaStatus.Compensated;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsWaitingForCallback()
    {
        Status = SagaStatus.WaitingForCallback;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkStepAsRunning(string stepName)
    {
        var step = Steps.First(s => s.Name == stepName);
        step.Status = SagaStepStatus.Running;
        step.StartedAt = DateTime.UtcNow;
    }

    public void MarkStepAsCompleted(string stepName)
    {
        var step = Steps.First(s => s.Name == stepName);
        step.Status = SagaStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
    }

    public void MarkStepAsFailed(string stepName, string errorMessage)
    {
        var step = Steps.First(s => s.Name == stepName);
        step.Status = SagaStepStatus.Failed;
        step.ErrorMessage = errorMessage;
        step.CompletedAt = DateTime.UtcNow;
    }

    public void Schedule(DateTime? scheduledFor)
    {
        Status = SagaStatus.Scheduled;
        UpdatedAt = DateTime.UtcNow;
        ScheduledFor = scheduledFor;
    }

    public IEnumerable<SagaStep> GetStepsNeedingCompensation()
    {
        return Steps
            .Where(s => s is { Status: SagaStepStatus.Completed, CanCompensate: true })
            .Reverse();
    }
}

public enum SagaStatus
{
    Pending = 0,
    Scheduled = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    FailedAfterPonr = 5,
    Compensating = 6,
    Compensated = 7,
    WaitingForCallback = 8,
}