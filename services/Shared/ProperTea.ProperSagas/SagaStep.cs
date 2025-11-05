namespace ProperTea.ProperSagas;

public class SagaStep
{
    public string Name { get; set; } = string.Empty;
    public SagaStepStatus Status { get; set; } = SagaStepStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Indicates if this is a pre-validation step (read-only, no compensation needed)
    /// Pre-validation steps can be used by front-end before starting the saga
    /// </summary>
    public bool IsPreValidation { get; set; }
    
    /// <summary>
    /// Indicates if this step has a compensation action defined
    /// If false, compensation will be skipped for this step
    /// </summary>
    public bool HasCompensation { get; set; } = true;
    
    /// <summary>
    /// Optional: Name of the compensation action for this step
    /// If not set, compensation handler will use convention (e.g., "Undo{StepName}")
    /// </summary>
    public string? CompensationName { get; set; }
}