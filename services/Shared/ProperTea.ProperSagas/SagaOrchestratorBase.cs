using Microsoft.Extensions.Logging;

namespace ProperTea.ProperSagas;

public abstract class SagaOrchestratorBase<TSaga> where TSaga : SagaBase
{
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger _logger;

    protected SagaOrchestratorBase(ISagaRepository sagaRepository, ILogger logger)
    {
        _sagaRepository = sagaRepository;
        _logger = logger;
    }

    public abstract Task<TSaga> StartAsync(TSaga saga);
    
    protected abstract Task ExecuteStepsAsync(TSaga saga);
    
    protected abstract Task CompensateAsync(TSaga saga);

    /// <summary>
    /// Optional: Execute only pre-validation steps (for front-end validation)
    /// Override this if you want to expose validation before saga execution
    /// </summary>
    public virtual async Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(TSaga saga)
    {
        _logger.LogInformation("Running pre-validation for saga type {SagaType}", saga.SagaType);
        
        var validationSteps = saga.GetPreValidationSteps().ToList();
        
        if (!validationSteps.Any())
        {
            _logger.LogWarning("No pre-validation steps defined for saga {SagaType}", saga.SagaType);
            return (true, null);
        }

        foreach (var step in validationSteps)
        {
            if (!await ExecuteStepAsync(saga, step.Name, async () => 
            {
                await ValidateStepAsync(saga, step.Name);
            }))
            {
                return (false, $"Validation failed at step: {step.Name}. {step.ErrorMessage}");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Override this to implement validation logic for specific steps
    /// This is called by ValidateAsync for each pre-validation step
    /// </summary>
    protected virtual Task ValidateStepAsync(TSaga saga, string stepName)
    {
        throw new NotImplementedException(
            $"Validation for step '{stepName}' is not implemented. " +
            $"Override ValidateStepAsync in your orchestrator to provide validation logic.");
    }

    /// <summary>
    /// Resume a saga from its last completed step (after crash or callback)
    /// </summary>
    public virtual async Task<TSaga> ResumeAsync(Guid sagaId)
    {
        var saga = await _sagaRepository.GetByIdAsync<TSaga>(sagaId);
        if (saga == null)
            throw new InvalidOperationException($"Saga {sagaId} not found");

        // Don't resume if already finished
        if (saga.Status == SagaStatus.Completed || saga.Status == SagaStatus.Compensated)
        {
            _logger.LogInformation("Saga {SagaId} already finished with status {Status}", 
                sagaId, saga.Status);
            return saga;
        }

        _logger.LogInformation("Resuming saga {SagaId} from status {Status}", sagaId, saga.Status);

        // Mark as running and continue execution
        saga.MarkAsRunning();
        await _sagaRepository.UpdateAsync(saga);

        await ExecuteStepsAsync(saga);
        return saga;
    }

    /// <summary>
    /// Execute a saga step with automatic error handling and state persistence
    /// </summary>
    protected async Task<bool> ExecuteStepAsync(TSaga saga, string stepName, Func<Task> action)
    {
        try
        {
            _logger.LogInformation("Executing saga step: {StepName} for saga {SagaId}", stepName, saga.Id);
            
            saga.MarkStepAsRunning(stepName);
            await _sagaRepository.UpdateAsync(saga);

            await action();

            saga.MarkStepAsCompleted(stepName);
            await _sagaRepository.UpdateAsync(saga);

            _logger.LogInformation("Completed saga step: {StepName} for saga {SagaId}", stepName, saga.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed saga step: {StepName} for saga {SagaId}", stepName, saga.Id);
            
            saga.MarkStepAsFailed(stepName, ex.Message);
            await _sagaRepository.UpdateAsync(saga);
            
            return false;
        }
    }

    /// <summary>
    /// Optional: Automatic compensation helper that compensates all completed steps
    /// Call this from your CompensateAsync implementation, or implement custom logic
    /// </summary>
    protected async Task CompensateCompletedAsync(TSaga saga, Func<TSaga, string, Task> compensationAction)
    {
        _logger.LogInformation("Starting automatic compensation for saga {SagaId}", saga.Id);
        
        saga.MarkAsCompensating();
        await _sagaRepository.UpdateAsync(saga);

        var stepsToCompensate = saga.GetStepsNeedingCompensation().ToList();

        if (!stepsToCompensate.Any())
        {
            _logger.LogInformation("No steps need compensation for saga {SagaId}", saga.Id);
            saga.MarkAsCompensated();
            await _sagaRepository.UpdateAsync(saga);
            return;
        }

        foreach (var step in stepsToCompensate)
        {
            try
            {
                var compensationName = step.CompensationName ?? $"Compensate{step.Name}";
                _logger.LogInformation("Compensating step {StepName} (action: {CompensationName}) for saga {SagaId}", 
                    step.Name, compensationName, saga.Id);

                await compensationAction(saga, step.Name);

                step.Status = SagaStepStatus.Compensated;
                await _sagaRepository.UpdateAsync(saga);

                _logger.LogInformation("Successfully compensated step {StepName} for saga {SagaId}", 
                    step.Name, saga.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Compensation failed for step {StepName} in saga {SagaId}. Manual intervention may be required.", 
                    step.Name, saga.Id);
                
                // Continue with other compensations even if one fails
                step.ErrorMessage = $"Compensation failed: {ex.Message}";
                await _sagaRepository.UpdateAsync(saga);
            }
        }

        saga.MarkAsCompensated();
        await _sagaRepository.UpdateAsync(saga);
    }
}