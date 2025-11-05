using System.Text.Json;

namespace ProperTea.ProperSagas;

public abstract class SagaBase
{
    protected SagaBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        SagaType = GetType().Name;
    }

    public Guid Id { get; protected set; }
    public string SagaType { get; protected set; }
    public SagaStatus Status { get; protected set; } = SagaStatus.NotStarted;
    public List<SagaStep> Steps { get; protected set; } = [];
    public string? ErrorMessage { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? CompletedAt { get; protected set; }
    public string SagaData { get; protected set; } = "{}"; // JSON serialized data

    public void MarkAsRunning()
    {
        Status = SagaStatus.Running;
    }

    public void MarkAsCompleted()
    {
        Status = SagaStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = SagaStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensating()
    {
        Status = SagaStatus.Compensating;
    }

    public void MarkAsCompensated()
    {
        Status = SagaStatus.Compensated;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsWaitingForCallback(string waitingFor)
    {
        Status = SagaStatus.WaitingForCallback;
        SetData("waitingFor", waitingFor);
    }

    public void MarkStepAsRunning(string stepName)
    {
        var step = Steps.FirstOrDefault(s => s.Name == stepName);
        if (step != null)
        {
            step.Status = SagaStepStatus.Running;
            step.StartedAt = DateTime.UtcNow;
        }
    }

    public void MarkStepAsCompleted(string stepName)
    {
        var step = Steps.FirstOrDefault(s => s.Name == stepName);
        if (step != null)
        {
            step.Status = SagaStepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
        }
    }

    public void MarkStepAsFailed(string stepName, string errorMessage)
    {
        var step = Steps.FirstOrDefault(s => s.Name == stepName);
        if (step != null)
        {
            step.Status = SagaStepStatus.Failed;
            step.ErrorMessage = errorMessage;
            step.CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Store strongly-typed data in the saga
    /// </summary>
    public void SetData<T>(string key, T value)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(SagaData)
                   ?? new Dictionary<string, object>();

        data[key] = value!;
        SagaData = JsonSerializer.Serialize(data);
    }

    /// <summary>
    ///     Retrieve strongly-typed data from the saga
    /// </summary>
    public T? GetData<T>(string key)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SagaData);
        if (data?.TryGetValue(key, out var element) == true)
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        return default;
    }

    /// <summary>
    ///     Check if saga has data for a key
    /// </summary>
    public bool HasData(string key)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SagaData);
        return data?.ContainsKey(key) ?? false;
    }

    /// <summary>
    ///     Get all pre-validation steps (read-only checks that don't need compensation)
    /// </summary>
    public IEnumerable<SagaStep> GetPreValidationSteps()
    {
        return Steps.Where(s => s.IsPreValidation);
    }

    /// <summary>
    ///     Get all execution steps (non-validation steps that may need compensation)
    /// </summary>
    public IEnumerable<SagaStep> GetExecutionSteps()
    {
        return Steps.Where(s => !s.IsPreValidation);
    }

    /// <summary>
    ///     Get completed steps that need compensation (in reverse order)
    /// </summary>
    public IEnumerable<SagaStep> GetStepsNeedingCompensation()
    {
        return Steps
            .Where(s => !s.IsPreValidation &&
                        s.HasCompensation &&
                        s.Status == SagaStepStatus.Completed)
            .Reverse();
    }

    /// <summary>
    ///     Check if all pre-validation steps are completed
    /// </summary>
    public bool AllPreValidationStepsCompleted()
    {
        var preValidationSteps = GetPreValidationSteps().ToList();
        return preValidationSteps.Any() &&
               preValidationSteps.All(s => s.Status == SagaStepStatus.Completed);
    }
}