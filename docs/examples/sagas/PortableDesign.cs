using ProperTea.ProperSagas;

namespace Examples.Sagas.Portable;

/// <summary>
/// Design pattern for sagas that can be easily migrated to Durable Functions
/// Key principles:
/// 1. Keep saga state simple and serializable
/// 2. Extract business logic to separate services
/// 3. Make steps small and focused
/// 4. Use primitive data types in saga data
/// </summary>

// ============================================
// PRINCIPLE 1: Simple, Serializable State
// ============================================

public class PortableGDPRDeletionSaga : SagaBase
{
    public PortableGDPRDeletionSaga()
    {
        Steps = new List<SagaStep>
        {
            new() { Name = "ValidateLeases", IsPreValidation = true },
            new() { Name = "ValidateInvoices", IsPreValidation = true },
            new() { Name = "CreateBackup" },
            new() { Name = "AnonymizeContact", HasCompensation = true },
            new() { Name = "DeactivateUser", HasCompensation = true }
        };
    }

    // ✅ Use primitive types only (easily serializable)
    public void SetInput(Guid userId, Guid organizationId)
    {
        SetData("userId", userId);
        SetData("organizationId", organizationId);
    }

    public Guid GetUserId() => GetData<Guid>("userId");
    public Guid GetOrganizationId() => GetData<Guid>("organizationId");
    
    // ✅ Store only IDs, not complex objects
    public void SetBackupId(string backupId) => SetData("backupId", backupId);
    public string? GetBackupId() => GetData<string>("backupId");
}

// ============================================
// PRINCIPLE 2: Business Logic in Services
// (These services can be reused in Durable Functions)
// ============================================

public interface IGDPRValidationService
{
    Task<ValidationResult> ValidateLeasesAsync(Guid userId, Guid organizationId);
    Task<ValidationResult> ValidateInvoicesAsync(Guid userId, Guid organizationId);
}

public interface IGDPRExecutionService
{
    Task<string> CreateBackupAsync(Guid userId, Guid organizationId);
    Task<Guid> AnonymizeContactAsync(Guid userId, Guid organizationId);
    Task DeactivateUserAsync(Guid userId);
}

public interface IGDPRCompensationService
{
    Task RestoreFromBackupAsync(string backupId);
    Task ReactivateUserAsync(Guid userId);
}

public record ValidationResult(bool IsValid, string? ErrorMessage);

// ============================================
// PRINCIPLE 3: Thin Orchestrator
// (Just coordinates services, minimal logic)
// ============================================

public class PortableGDPRDeletionOrchestrator : SagaOrchestratorBase<PortableGDPRDeletionSaga>
{
    private readonly IGDPRValidationService _validationService;
    private readonly IGDPRExecutionService _executionService;
    private readonly IGDPRCompensationService _compensationService;

    public PortableGDPRDeletionOrchestrator(
        ISagaRepository sagaRepository,
        ILogger<PortableGDPRDeletionOrchestrator> logger,
        IGDPRValidationService validationService,
        IGDPRExecutionService executionService,
        IGDPRCompensationService compensationService)
        : base(sagaRepository, logger)
    {
        _validationService = validationService;
        _executionService = executionService;
        _compensationService = compensationService;
    }

    protected override async Task ValidateStepAsync(PortableGDPRDeletionSaga saga, string stepName)
    {
        var userId = saga.GetUserId();
        var orgId = saga.GetOrganizationId();

        // ✅ Just call service, minimal orchestration logic
        var result = stepName switch
        {
            "ValidateLeases" => await _validationService.ValidateLeasesAsync(userId, orgId),
            "ValidateInvoices" => await _validationService.ValidateInvoicesAsync(userId, orgId),
            _ => throw new InvalidOperationException($"Unknown validation step: {stepName}")
        };

        if (!result.IsValid)
            throw new InvalidOperationException(result.ErrorMessage);
    }

    public override async Task<PortableGDPRDeletionSaga> StartAsync(PortableGDPRDeletionSaga saga)
    {
        saga.MarkAsRunning();
        await _sagaRepository.SaveAsync(saga);
        await ExecuteStepsAsync(saga);
        return saga;
    }

    protected override async Task ExecuteStepsAsync(PortableGDPRDeletionSaga saga)
    {
        var userId = saga.GetUserId();
        var orgId = saga.GetOrganizationId();

        // Validation phase
        foreach (var step in saga.GetPreValidationSteps())
        {
            if (!await ExecuteStepAsync(saga, step.Name, async () =>
            {
                await ValidateStepAsync(saga, step.Name);
            }))
            {
                saga.MarkAsFailed($"Validation failed: {step.Name}");
                await _sagaRepository.UpdateAsync(saga);
                return;
            }
        }

        // Execution phase - each step is a simple service call
        if (!await ExecuteStepAsync(saga, "CreateBackup", async () =>
        {
            var backupId = await _executionService.CreateBackupAsync(userId, orgId);
            saga.SetBackupId(backupId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        if (!await ExecuteStepAsync(saga, "AnonymizeContact", async () =>
        {
            await _executionService.AnonymizeContactAsync(userId, orgId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        if (!await ExecuteStepAsync(saga, "DeactivateUser", async () =>
        {
            await _executionService.DeactivateUserAsync(userId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        saga.MarkAsCompleted();
        await _sagaRepository.UpdateAsync(saga);
    }

    protected override async Task CompensateAsync(PortableGDPRDeletionSaga saga)
    {
        await CompensateCompletedAsync(saga, async (s, stepName) =>
        {
            var userId = s.GetUserId();

            switch (stepName)
            {
                case "AnonymizeContact":
                    var backupId = s.GetBackupId();
                    if (!string.IsNullOrEmpty(backupId))
                        await _compensationService.RestoreFromBackupAsync(backupId);
                    break;

                case "DeactivateUser":
                    await _compensationService.ReactivateUserAsync(userId);
                    break;
            }
        });
    }
}

// ============================================
// FUTURE: Durable Functions Version
// (Notice how services are reused!)
// ============================================

/*
[FunctionName("GDPRDeletionOrchestrator")]
public async Task<string> RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var input = context.GetInput<GDPRDeletionInput>();
    
    try
    {
        // Validation phase - same services!
        var leasesValid = await context.CallActivityAsync<bool>("ValidateLeases", 
            new { input.UserId, input.OrganizationId });
        if (!leasesValid) throw new Exception("Active leases exist");
        
        var invoicesValid = await context.CallActivityAsync<bool>("ValidateInvoices",
            new { input.UserId, input.OrganizationId });
        if (!invoicesValid) throw new Exception("Unpaid invoices exist");
        
        // Execution phase - same services!
        var backupId = await context.CallActivityAsync<string>("CreateBackup",
            new { input.UserId, input.OrganizationId });
        
        await context.CallActivityAsync("AnonymizeContact",
            new { input.UserId, input.OrganizationId });
        
        await context.CallActivityAsync("DeactivateUser", input.UserId);
        
        return "Completed";
    }
    catch (Exception)
    {
        // Compensation
        await context.CallActivityAsync("CompensateGDPRDeletion", input);
        throw;
    }
}

// Activities wrap your existing services
[FunctionName("ValidateLeases")]
public async Task<bool> ValidateLeases(
    [ActivityTrigger] IDurableActivityContext context,
    [Inject] IGDPRValidationService validationService)
{
    var input = context.GetInput<dynamic>();
    var result = await validationService.ValidateLeasesAsync(
        (Guid)input.UserId, (Guid)input.OrganizationId);
    return result.IsValid;
}

[FunctionName("CreateBackup")]
public async Task<string> CreateBackup(
    [ActivityTrigger] IDurableActivityContext context,
    [Inject] IGDPRExecutionService executionService)
{
    var input = context.GetInput<dynamic>();
    return await executionService.CreateBackupAsync(
        (Guid)input.UserId, (Guid)input.OrganizationId);
}

// etc...
*/

public record GDPRDeletionInput(Guid UserId, Guid OrganizationId);

