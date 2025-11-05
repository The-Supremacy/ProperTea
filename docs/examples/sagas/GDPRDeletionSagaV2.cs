using ProperTea.ProperSagas;

namespace Examples.Sagas;

/// <summary>
/// Updated GDPR deletion saga with pre-validation steps
/// This demonstrates the new validation and compensation capabilities
/// </summary>
public class GDPRDeletionSagaV2 : SagaBase
{
    public GDPRDeletionSagaV2()
    {
        Steps = new List<SagaStep>
        {
            // PRE-VALIDATION STEPS (read-only, front-end can call these)
            new() 
            { 
                Name = "ValidateLeases", 
                Status = SagaStepStatus.Pending,
                IsPreValidation = true,  // ✅ This is a validation step
                HasCompensation = false  // ❌ No compensation needed for read-only checks
            },
            new() 
            { 
                Name = "ValidateInvoices", 
                Status = SagaStepStatus.Pending,
                IsPreValidation = true,
                HasCompensation = false
            },
            new() 
            { 
                Name = "ValidateDataDependencies", 
                Status = SagaStepStatus.Pending,
                IsPreValidation = true,
                HasCompensation = false
            },
            
            // EXECUTION STEPS (modify data, need compensation)
            new() 
            { 
                Name = "BackupContact", 
                Status = SagaStepStatus.Pending,
                HasCompensation = false  // Backup doesn't need compensation
            },
            new() 
            { 
                Name = "AnonymizeContact", 
                Status = SagaStepStatus.Pending,
                HasCompensation = true,  // ✅ Can be rolled back from backup
                CompensationName = "RestoreContactFromBackup"
            },
            new() 
            { 
                Name = "DeactivateUser", 
                Status = SagaStepStatus.Pending,
                HasCompensation = true,
                CompensationName = "ReactivateUser"
            },
            new() 
            { 
                Name = "RemoveGroupMemberships", 
                Status = SagaStepStatus.Pending,
                HasCompensation = false  // Point of no return - cannot easily restore
            },
            new() 
            { 
                Name = "PurgePersonalData", 
                Status = SagaStepStatus.Pending,
                HasCompensation = false  // Permanent deletion
            }
        };
    }

    // Strongly-typed helper methods
    public void SetUserId(Guid userId) => SetData("userId", userId);
    public Guid GetUserId() => GetData<Guid>("userId");
    
    public void SetOrganizationId(Guid organizationId) => SetData("organizationId", organizationId);
    public Guid GetOrganizationId() => GetData<Guid>("organizationId");
    
    public void SetContactId(Guid contactId) => SetData("contactId", contactId);
    public Guid? GetContactId() => GetData<Guid?>("contactId");
    
    public void SetBackupId(string backupId) => SetData("backupId", backupId);
    public string? GetBackupId() => GetData<string>("backupId");
}

