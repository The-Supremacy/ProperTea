using ProperTea.ProperSagas;
using Microsoft.Extensions.Logging;

namespace Examples.Sagas;

/// <summary>
/// Example orchestrator for GDPR data deletion workflow
/// This is a reference example - adapt to your needs
/// </summary>
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    private readonly ILeaseService _leaseService;
    private readonly IInvoiceService _invoiceService;
    private readonly IContactService _contactService;
    private readonly IIdentityService _identityService;
    private readonly IPermissionService _permissionService;

    public GDPRDeletionOrchestrator(
        ISagaRepository sagaRepository,
        ILogger<GDPRDeletionOrchestrator> logger,
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

    public override async Task<GDPRDeletionSaga> StartAsync(GDPRDeletionSaga saga)
    {
        saga.MarkAsRunning();
        await _sagaRepository.SaveAsync(saga);

        await ExecuteStepsAsync(saga);
        return saga;
    }

    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        var userId = saga.GetUserId();
        var organizationId = saga.GetOrganizationId();

        // ===== VALIDATION PHASE (Read-only, no compensation needed) =====
        
        if (!await ExecuteStepAsync(saga, "ValidateLeases", async () =>
        {
            var hasActiveLeases = await _leaseService.HasActiveLeasesAsync(userId, organizationId);
            if (hasActiveLeases)
                throw new InvalidOperationException("Cannot delete user with active leases");
        }))
        {
            saga.MarkAsFailed("User has active leases");
            await _sagaRepository.UpdateAsync(saga);
            return; // No compensation for validation failures
        }

        if (!await ExecuteStepAsync(saga, "ValidateInvoices", async () =>
        {
            var hasUnpaidInvoices = await _invoiceService.HasUnpaidInvoicesAsync(userId, organizationId);
            if (hasUnpaidInvoices)
                throw new InvalidOperationException("Cannot delete user with unpaid invoices");
        }))
        {
            saga.MarkAsFailed("User has unpaid invoices");
            await _sagaRepository.UpdateAsync(saga);
            return;
        }

        // ===== EXECUTION PHASE (Writes data - needs compensation if fails) =====
        
        if (!await ExecuteStepAsync(saga, "AnonymizeContact", async () =>
        {
            var contactId = await _contactService.AnonymizeAsync(userId, organizationId);
            saga.SetContactId(contactId);
        }))
        {
            await CompensateAsync(saga); // Trigger compensation
            return;
        }

        if (!await ExecuteStepAsync(saga, "DeactivateUser", async () =>
        {
            await _identityService.DeactivateUserAsync(userId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        if (!await ExecuteStepAsync(saga, "RemoveGroupMemberships", async () =>
        {
            await _permissionService.RemoveAllMembershipsAsync(userId, organizationId);
        }))
        {
            await CompensateAsync(saga);
            return;
        }

        // All steps completed successfully
        saga.MarkAsCompleted();
        await _sagaRepository.UpdateAsync(saga);
    }

    protected override async Task CompensateAsync(GDPRDeletionSaga saga)
    {
        saga.MarkAsCompensating();
        await _sagaRepository.UpdateAsync(saga);

        var userId = saga.GetUserId();
        var organizationId = saga.GetOrganizationId();

        // Rollback completed steps in REVERSE order
        var completedSteps = saga.Steps
            .Where(s => s.Status == SagaStepStatus.Completed)
            .Reverse()
            .ToList();

        foreach (var step in completedSteps)
        {
            try
            {
                switch (step.Name)
                {
                    case "RemoveGroupMemberships":
                        // Not easily reversible - log for manual review
                        _logger.LogWarning(
                            "Cannot automatically restore group memberships for user {UserId} in org {OrgId}. Manual review required.",
                            userId, organizationId);
                        break;

                    case "DeactivateUser":
                        await _identityService.ReactivateUserAsync(userId);
                        _logger.LogInformation("Compensated: Reactivated user {UserId}", userId);
                        break;

                    case "AnonymizeContact":
                        var contactId = saga.GetContactId();
                        if (contactId.HasValue)
                        {
                            await _contactService.RestoreFromBackupAsync(contactId.Value);
                            _logger.LogInformation("Compensated: Restored contact {ContactId}", contactId.Value);
                        }
                        break;
                }

                step.Status = SagaStepStatus.Compensated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Compensation failed for step {StepName} in saga {SagaId}. Manual intervention required.", 
                    step.Name, saga.Id);
                // Continue with other compensations even if one fails
            }
        }

        saga.MarkAsCompensated();
        await _sagaRepository.UpdateAsync(saga);
    }
}

// Example service interfaces (implement these in your actual services)
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
    Task<Guid> AnonymizeAsync(Guid userId, Guid organizationId);
    Task RestoreFromBackupAsync(Guid contactId);
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

