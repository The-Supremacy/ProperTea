using ProperTea.ProperSagas;

namespace Examples.Sagas.Hybrid;

/// <summary>
/// Hybrid approach: Steps can optionally contain implementation
/// This allows reusable step logic while maintaining simplicity
/// </summary>

// 1. Define step action interface
public interface ISagaStepAction<TSaga> where TSaga : SagaBase
{
    string StepName { get; }
    Task ExecuteAsync(TSaga saga, CancellationToken cancellationToken = default);
    Task CompensateAsync(TSaga saga, CancellationToken cancellationToken = default);
}

// 2. Example reusable step action
public class ValidateLeasesAction : ISagaStepAction<GDPRDeletionSaga>
{
    private readonly ILeaseService _leaseService;
    
    public string StepName => "ValidateLeases";
    
    public ValidateLeasesAction(ILeaseService leaseService)
    {
        _leaseService = leaseService;
    }
    
    public async Task ExecuteAsync(GDPRDeletionSaga saga, CancellationToken cancellationToken = default)
    {
        var userId = saga.GetUserId();
        var hasActiveLeases = await _leaseService.HasActiveLeasesAsync(userId);
        
        if (hasActiveLeases)
            throw new InvalidOperationException("Cannot delete user with active leases");
    }
    
    public async Task CompensateAsync(GDPRDeletionSaga saga, CancellationToken cancellationToken = default)
    {
        // No compensation needed for validation
        await Task.CompletedTask;
    }
}

// 3. Another reusable action
public class AnonymizeContactAction : ISagaStepAction<GDPRDeletionSaga>
{
    private readonly IContactService _contactService;
    
    public string StepName => "AnonymizeContact";
    
    public AnonymizeContactAction(IContactService contactService)
    {
        _contactService = contactService;
    }
    
    public async Task ExecuteAsync(GDPRDeletionSaga saga, CancellationToken cancellationToken = default)
    {
        var userId = saga.GetUserId();
        var contactId = await _contactService.AnonymizeAsync(userId, saga.GetOrganizationId());
        saga.SetContactId(contactId);
    }
    
    public async Task CompensateAsync(GDPRDeletionSaga saga, CancellationToken cancellationToken = default)
    {
        var backupId = saga.GetBackupId();
        if (!string.IsNullOrEmpty(backupId))
        {
            await _contactService.RestoreFromBackupAsync(backupId);
        }
    }
}

// 4. Enhanced orchestrator that uses step actions
public class GDPRDeletionOrchestratorHybrid : SagaOrchestratorBase<GDPRDeletionSaga>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _stepActions;
    
    public GDPRDeletionOrchestratorHybrid(
        ISagaRepository sagaRepository,
        ILogger<GDPRDeletionOrchestratorHybrid> logger,
        IServiceProvider serviceProvider)
        : base(sagaRepository, logger)
    {
        _serviceProvider = serviceProvider;
        
        // Register step actions (could be done via DI/configuration)
        _stepActions = new Dictionary<string, Type>
        {
            ["ValidateLeases"] = typeof(ValidateLeasesAction),
            ["AnonymizeContact"] = typeof(AnonymizeContactAction),
            // Add more as needed
        };
    }
    
    public override async Task<GDPRDeletionSaga> StartAsync(GDPRDeletionSaga saga)
    {
        saga.MarkAsRunning();
        await _sagaRepository.SaveAsync(saga);
        await ExecuteStepsAsync(saga);
        return saga;
    }
    
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        var steps = saga.GetExecutionSteps();
        
        foreach (var step in steps)
        {
            if (step.Status == SagaStepStatus.Completed)
                continue; // Skip already completed steps (for resume)
            
            // Check if we have a reusable action for this step
            if (_stepActions.TryGetValue(step.Name, out var actionType))
            {
                // Use the reusable action
                var action = (ISagaStepAction<GDPRDeletionSaga>)_serviceProvider.GetRequiredService(actionType);
                
                if (!await ExecuteStepAsync(saga, step.Name, async () =>
                {
                    await action.ExecuteAsync(saga);
                }))
                {
                    await CompensateAsync(saga);
                    return;
                }
            }
            else
            {
                // Fall back to inline implementation (for saga-specific logic)
                if (!await ExecuteInlineStepAsync(saga, step.Name))
                {
                    await CompensateAsync(saga);
                    return;
                }
            }
        }
        
        saga.MarkAsCompleted();
        await _sagaRepository.UpdateAsync(saga);
    }
    
    // Handle saga-specific steps that don't have reusable actions
    private async Task<bool> ExecuteInlineStepAsync(GDPRDeletionSaga saga, string stepName)
    {
        return stepName switch
        {
            "BackupContact" => await ExecuteStepAsync(saga, stepName, async () =>
            {
                // Inline implementation for saga-specific logic
                var contactService = _serviceProvider.GetRequiredService<IContactService>();
                var backupId = await contactService.CreateBackupAsync(saga.GetUserId(), saga.GetOrganizationId());
                saga.SetBackupId(backupId);
            }),
            
            _ => throw new InvalidOperationException($"No action registered for step: {stepName}")
        };
    }
    
    protected override async Task CompensateAsync(GDPRDeletionSaga saga)
    {
        var stepsToCompensate = saga.GetStepsNeedingCompensation();
        
        saga.MarkAsCompensating();
        await _sagaRepository.UpdateAsync(saga);
        
        foreach (var step in stepsToCompensate)
        {
            try
            {
                // Try to use reusable action for compensation
                if (_stepActions.TryGetValue(step.Name, out var actionType))
                {
                    var action = (ISagaStepAction<GDPRDeletionSaga>)_serviceProvider.GetRequiredService(actionType);
                    await action.CompensateAsync(saga);
                }
                else
                {
                    // Fall back to inline compensation
                    await CompensateInlineStepAsync(saga, step.Name);
                }
                
                step.Status = SagaStepStatus.Compensated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compensation failed for step {StepName}", step.Name);
                step.ErrorMessage = $"Compensation failed: {ex.Message}";
            }
        }
        
        saga.MarkAsCompensated();
        await _sagaRepository.UpdateAsync(saga);
    }
    
    private async Task CompensateInlineStepAsync(GDPRDeletionSaga saga, string stepName)
    {
        // Inline compensation for saga-specific steps
        switch (stepName)
        {
            case "BackupContact":
                // No compensation needed
                break;
            default:
                _logger.LogWarning("No compensation logic for step {StepName}", stepName);
                break;
        }
        
        await Task.CompletedTask;
    }
}

// 5. Registration in DI
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGDPRDeletionSaga(this IServiceCollection services)
    {
        // Register reusable step actions
        services.AddScoped<ValidateLeasesAction>();
        services.AddScoped<AnonymizeContactAction>();
        // Add more actions...
        
        // Register orchestrator
        services.AddScoped<GDPRDeletionOrchestratorHybrid>();
        
        return services;
    }
}

// Service interfaces
public interface ILeaseService
{
    Task<bool> HasActiveLeasesAsync(Guid userId);
}

public interface IContactService
{
    Task<Guid> AnonymizeAsync(Guid userId, Guid organizationId);
    Task RestoreFromBackupAsync(string backupId);
    Task<string> CreateBackupAsync(Guid userId, Guid organizationId);
}

