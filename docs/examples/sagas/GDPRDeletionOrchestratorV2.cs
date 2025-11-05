using ProperTea.ProperSagas;
using Microsoft.Extensions.Logging;

namespace Examples.Sagas;

/// <summary>
/// Updated orchestrator demonstrating pre-validation and automatic compensation
/// </summary>
public class GDPRDeletionOrchestratorV2 : SagaOrchestratorBase<GDPRDeletionSagaV2>
{
    private readonly ILeaseService _leaseService;
    private readonly IInvoiceService _invoiceService;
    private readonly IContactService _contactService;
    private readonly IIdentityService _identityService;
    private readonly IPermissionService _permissionService;

    public GDPRDeletionOrchestratorV2(
        ISagaRepository sagaRepository,
        ILogger<GDPRDeletionOrchestratorV2> logger,
        ILeaseService leaseService,
        IInvoiceService invoiceService,
        IContactService contactService,
        IIdentityService identityService,
        IPermissionService permissionService)
        : base(sagaRepository, logger)
    {
        _leaseService = leaseService;
        _invoiceService = invoiceService;
        _contactService = contactService;
        _identityService = identityService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Override ValidateStepAsync to provide validation logic for each pre-validation step
    /// This can be called from front-end BEFORE starting the saga
    /// </summary>
    protected override async Task ValidateStepAsync(GDPRDeletionSagaV2 saga, string stepName)
    {
        var userId = saga.GetUserId();
        var organizationId = saga.GetOrganizationId();

        switch (stepName)
        {
            case "ValidateLeases":
                var hasActiveLeases = await _leaseService.HasActiveLeasesAsync(userId, organizationId);
                if (hasActiveLeases)
                    throw new InvalidOperationException("Cannot delete user with active leases");
                break;

            case "ValidateInvoices":
                var hasUnpaidInvoices = await _invoiceService.HasUnpaidInvoicesAsync(userId, organizationId);
                if (hasUnpaidInvoices)
                    throw new InvalidOperationException("Cannot delete user with unpaid invoices");
                break;

            case "ValidateDataDependencies":
                var hasCriticalDependencies = await _contactService.HasCriticalDependenciesAsync(userId, organizationId);
                if (hasCriticalDependencies)
                    throw new InvalidOperationException("Cannot delete user: critical data dependencies exist");
                break;

            default:
                throw new InvalidOperationException($"Unknown validation step: {stepName}");
        }
    }

    public override async Task<GDPRDeletionSagaV2> StartAsync(GDPRDeletionSagaV2 saga)
    {
        saga.MarkAsRunning();
        await _sagaRepository.SaveAsync(saga);

        await ExecuteStepsAsync(saga);
        return saga;
    }

    protected override async Task ExecuteStepsAsync(GDPRDeletionSagaV2 saga)
    {
        var userId = saga.GetUserId();
        var organizationId = saga.GetOrganizationId();

        // ===== OPTION 1: Run validation inline (if not already done) =====
        var preValidationSteps = saga.GetPreValidationSteps().ToList();
        foreach (var step in preValidationSteps)
        {
            if (step.Status != SagaStepStatus.Completed)
            {
                if (!await ExecuteStepAsync(saga, step.Name, async () =>
                {
                    await ValidateStepAsync(saga, step.Name);
                }))
                {
                    saga.MarkAsFailed($"Validation failed at {step.Name}");
                    await _sagaRepository.UpdateAsync(saga);
                    return; // Stop execution, NO compensation needed
                }
            }
        }

        // ===== EXECUTION PHASE =====
        
        // Backup contact data (no compensation needed)
        if (!await ExecuteStepAsync(saga, "BackupContact", async () =>
        {
            var backupId = await _contactService.CreateBackupAsync(userId, organizationId);
            saga.SetBackupId(backupId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        // Anonymize contact (CAN be compensated from backup)
        if (!await ExecuteStepAsync(saga, "AnonymizeContact", async () =>
        {
            var contactId = await _contactService.AnonymizeAsync(userId, organizationId);
            saga.SetContactId(contactId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        // Deactivate user (CAN be compensated)
        if (!await ExecuteStepAsync(saga, "DeactivateUser", async () =>
        {
            await _identityService.DeactivateUserAsync(userId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        // ===== POINT OF NO RETURN =====
        // After this, compensation is not possible/practical

        // Remove group memberships (CANNOT be easily compensated)
        if (!await ExecuteStepAsync(saga, "RemoveGroupMemberships", async () =>
        {
            await _permissionService.RemoveAllMembershipsAsync(userId, organizationId);
        }))
        {
            // At this point, we cannot fully compensate
            // Log for manual intervention
            _logger.LogCritical(
                "GDPR deletion saga {SagaId} failed after point of no return. Manual data review required for user {UserId}",
                saga.Id, userId);
            
            saga.MarkAsFailed("Failed after point of no return - manual intervention required");
            await _sagaRepository.UpdateAsync(saga);
            return;
        }

        // Purge personal data (permanent deletion)
        if (!await ExecuteStepAsync(saga, "PurgePersonalData", async () =>
        {
            await _contactService.PurgePersonalDataAsync(userId, organizationId);
        }))
        {
            _logger.LogCritical(
                "GDPR deletion saga {SagaId} failed during final purge for user {UserId}. Data may be in inconsistent state.",
                saga.Id, userId);
            
            saga.MarkAsFailed("Failed during final purge - data inconsistency");
            await _sagaRepository.UpdateAsync(saga);
            return;
        }

        // All steps completed
        saga.MarkAsCompleted();
        await _sagaRepository.UpdateAsync(saga);
    }

    protected override async Task CompensateAsync(GDPRDeletionSagaV2 saga)
    {
        // ===== OPTION A: Use automatic compensation helper =====
        await AutoCompensateAsync(saga, async (s, stepName) =>
        {
            var userId = s.GetUserId();
            var organizationId = s.GetOrganizationId();

            switch (stepName)
            {
                case "AnonymizeContact":
                    var backupId = s.GetBackupId();
                    if (!string.IsNullOrEmpty(backupId))
                    {
                        await _contactService.RestoreFromBackupAsync(backupId);
                    }
                    break;

                case "DeactivateUser":
                    await _identityService.ReactivateUserAsync(userId);
                    break;

                // BackupContact doesn't need compensation (it's just a backup)
                // Other steps have HasCompensation = false so they're skipped
            }
        });

        // ===== OPTION B: Manual compensation (if you need more control) =====
        /*
        saga.MarkAsCompensating();
        await _sagaRepository.UpdateAsync(saga);

        var userId = saga.GetUserId();
        var organizationId = saga.GetOrganizationId();

        // Get only steps that need compensation
        var stepsToCompensate = saga.GetStepsNeedingCompensation().ToList();

        foreach (var step in stepsToCompensate)
        {
            try
            {
                switch (step.Name)
                {
                    case "AnonymizeContact":
                        var backupId = saga.GetBackupId();
                        if (!string.IsNullOrEmpty(backupId))
                        {
                            await _contactService.RestoreFromBackupAsync(backupId);
                        }
                        break;

                    case "DeactivateUser":
                        await _identityService.ReactivateUserAsync(userId);
                        break;
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
        */
    }
}

// Service interfaces (same as before)
public interface ILeaseService
{
    Task<bool> HasActiveLeasesAsync(Guid userId, Guid organizationId);
}

public interface IInvoiceService
{
    Task<bool> HasUnpaidInvoicesAsync(Guid userId, Guid organizationId);
}

public interface IContactService
{
    Task<bool> HasCriticalDependenciesAsync(Guid userId, Guid organizationId);
    Task<string> CreateBackupAsync(Guid userId, Guid organizationId);
    Task<Guid> AnonymizeAsync(Guid userId, Guid organizationId);
    Task RestoreFromBackupAsync(string backupId);
    Task PurgePersonalDataAsync(Guid userId, Guid organizationId);
}

public interface IIdentityService
{
    Task DeactivateUserAsync(Guid userId);
    Task ReactivateUserAsync(Guid userId);
}

public interface IPermissionService
{
    Task RemoveAllMembershipsAsync(Guid userId, Guid organizationId);
}

