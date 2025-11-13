namespace TheSupremacy.ProperSagas.Domain;

public class SagaStep
{
    public string Name { get; set; } = string.Empty;
    public SagaStepStatus Status { get; set; } = SagaStepStatus.Pending;
    public SagaStepType Type { get; set; } = SagaStepType.Execution;
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompensationName { get; set; }

    public bool CanCompensate =>
        Type != SagaStepType.NoCompensation &&
        Type != SagaStepType.PointOfNoReturn &&
        !string.IsNullOrEmpty(CompensationName);
}

public enum SagaStepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Compensated = 4
}

public enum SagaStepType
{
    Execution = 0,
    NoCompensation = 1,
    PointOfNoReturn = 2
}