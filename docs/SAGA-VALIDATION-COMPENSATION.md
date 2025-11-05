# Saga Validation and Compensation Guide

**Version:** 2.0.0  
**Last Updated:** October 31, 2025

---

## Overview

ProperSagas supports two advanced patterns for real-world saga implementations:

1. **Pre-Validation** - Read-only checks that can be run before starting a saga (even from front-end)
2. **Flexible Compensation** - Per-step compensation configuration with "point of no return" support

---

## 1. Pre-Validation Pattern

### The Problem

In multi-step sagas, you often want to validate preconditions BEFORE executing any changes:

- ✅ Front-end can show validation errors before user confirms action
- ✅ Avoid starting a saga that will fail validation
- ✅ Separate read-only checks from write operations

### The Solution: `IsPreValidation` Flag

Mark steps as pre-validation to separate them from execution steps:

```csharp
public class GDPRDeletionSaga : SagaBase
{
    public GDPRDeletionSaga()
    {
        Steps = new List<SagaStep>
        {
            // PRE-VALIDATION (read-only checks)
            new() 
            { 
                Name = "ValidateLeases",
                IsPreValidation = true,  // ✅ Mark as validation
                HasCompensation = false  // ❌ No compensation needed
            },
            new() 
            { 
                Name = "ValidateInvoices",
                IsPreValidation = true,
                HasCompensation = false
            },
            
            // EXECUTION (modify data)
            new() 
            { 
                Name = "AnonymizeContact",
                IsPreValidation = false,  // This is an execution step
                HasCompensation = true    // ✅ Can be compensated
            }
        };
    }
}
```

### Front-End Validation Endpoint

```csharp
// Endpoint that runs ONLY pre-validation steps
app.MapPost("/api/gdpr/delete-request/validate", async (
    GDPRDeletionRequest request,
    GDPRDeletionOrchestrator orchestrator) =>
{
    var saga = new GDPRDeletionSaga();
    saga.SetUserId(request.UserId);
    
    // Runs ONLY steps where IsPreValidation = true
    var (isValid, errorMessage) = await orchestrator.ValidateAsync(saga);
    
    if (!isValid)
        return Results.BadRequest(new { error = errorMessage });
    
    return Results.Ok(new { message = "Validation passed" });
});
```

### Implementing Validation Logic

Override `ValidateStepAsync` in your orchestrator:

```csharp
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    protected override async Task ValidateStepAsync(GDPRDeletionSaga saga, string stepName)
    {
        var userId = saga.GetUserId();

        switch (stepName)
        {
            case "ValidateLeases":
                var hasActiveLeases = await _leaseService.HasActiveLeasesAsync(userId);
                if (hasActiveLeases)
                    throw new InvalidOperationException("Cannot delete: active leases exist");
                break;

            case "ValidateInvoices":
                var hasUnpaidInvoices = await _invoiceService.HasUnpaidInvoicesAsync(userId);
                if (hasUnpaidInvoices)
                    throw new InvalidOperationException("Cannot delete: unpaid invoices exist");
                break;

            default:
                throw new InvalidOperationException($"Unknown validation step: {stepName}");
        }
    }
}
```

### Helper Methods in SagaBase

```csharp
// Get all pre-validation steps
var validationSteps = saga.GetPreValidationSteps();

// Get all execution steps (non-validation)
var executionSteps = saga.GetExecutionSteps();

// Check if all validations completed
bool allValid = saga.AllPreValidationStepsCompleted();
```

### Front-End Usage Flow

```typescript
// Step 1: Validate before showing confirmation dialog
const response = await fetch('/api/gdpr/delete-request/validate', {
    method: 'POST',
    body: JSON.stringify({ userId, organizationId })
});

if (!response.ok) {
    const errors = await response.json();
    // Show validation errors to user
    alert(`Cannot delete: ${errors.error}`);
    return;
}

// Step 2: Show confirmation dialog
if (confirm('All validation passed. Proceed with deletion?')) {
    // Step 3: Start actual saga
    await fetch('/api/gdpr/delete-request', {
        method: 'POST',
        body: JSON.stringify({ userId, organizationId })
    });
}
```

---

## 2. Compensation Pattern

### The Problem

Different saga steps have different compensation requirements:

- ✅ Some steps are read-only (no compensation needed)
- ✅ Some steps can be easily compensated
- ✅ Some steps reach a "point of no return" (cannot be compensated)

### The Solution: Flexible Step Configuration

Configure each step's compensation behavior:

```csharp
public class GDPRDeletionSaga : SagaBase
{
    public GDPRDeletionSaga()
    {
        Steps = new List<SagaStep>
        {
            // Validation steps - no compensation
            new() 
            { 
                Name = "ValidateLeases",
                IsPreValidation = true,
                HasCompensation = false  // Read-only, no compensation
            },
            
            // Backup step - no compensation needed (it's just a backup)
            new() 
            { 
                Name = "BackupContact",
                HasCompensation = false
            },
            
            // Can be compensated from backup
            new() 
            { 
                Name = "AnonymizeContact",
                HasCompensation = true,
                CompensationName = "RestoreFromBackup"  // Optional: custom name
            },
            
            // Can be compensated
            new() 
            { 
                Name = "DeactivateUser",
                HasCompensation = true,
                CompensationName = "ReactivateUser"
            },
            
            // POINT OF NO RETURN - cannot be compensated
            new() 
            { 
                Name = "PurgePersonalData",
                HasCompensation = false  // Permanent deletion
            }
        };
    }
}
```

### Option A: Automatic Compensation

Use the built-in `AutoCompensateAsync` helper:

```csharp
protected override async Task CompensateAsync(GDPRDeletionSaga saga)
{
    // Automatically compensates all steps where HasCompensation = true
    // Skips steps where HasCompensation = false
    // Processes in REVERSE order
    await AutoCompensateAsync(saga, async (s, stepName) =>
    {
        switch (stepName)
        {
            case "AnonymizeContact":
                var backupId = s.GetBackupId();
                await _contactService.RestoreFromBackupAsync(backupId);
                break;

            case "DeactivateUser":
                var userId = s.GetUserId();
                await _identityService.ReactivateUserAsync(userId);
                break;
        }
    });
}
```

**What `AutoCompensateAsync` does:**

1. Marks saga as `Compensating`
2. Gets steps where `HasCompensation = true` and `Status = Completed` (in REVERSE order)
3. For each step, calls your compensation action
4. Marks step as `Compensated` on success
5. Logs error and continues on failure (doesn't stop other compensations)
6. Marks saga as `Compensated` when done

### Option B: Manual Compensation

For complete control:

```csharp
protected override async Task CompensateAsync(GDPRDeletionSaga saga)
{
    saga.MarkAsCompensating();
    await _sagaRepository.UpdateAsync(saga);

    var userId = saga.GetUserId();

    // Get only steps that need compensation (in reverse order)
    var stepsToCompensate = saga.GetStepsNeedingCompensation();

    foreach (var step in stepsToCompensate)
    {
        try
        {
            switch (step.Name)
            {
                case "AnonymizeContact":
                    await _contactService.RestoreFromBackupAsync(saga.GetBackupId());
                    break;

                case "DeactivateUser":
                    await _identityService.ReactivateUserAsync(userId);
                    break;
            }

            step.Status = SagaStepStatus.Compensated;
            await _sagaRepository.UpdateAsync(saga);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compensation failed for {StepName}", step.Name);
            step.ErrorMessage = $"Compensation failed: {ex.Message}";
            // Continue with other compensations
        }
    }

    saga.MarkAsCompensated();
    await _sagaRepository.UpdateAsync(saga);
}
```

### Helper Methods for Compensation

```csharp
// Get steps that need compensation (HasCompensation = true, Status = Completed)
// Returns in REVERSE order for proper rollback
var stepsToCompensate = saga.GetStepsNeedingCompensation();
```

---

## 3. Point of No Return Pattern

### The Problem

Some steps cannot be easily compensated (e.g., permanent data deletion, external notifications sent).

### The Solution: Separate Reversible from Irreversible Steps

```csharp
protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
{
    // Phase 1: Validation (no writes)
    // ... validation logic ...

    // Phase 2: Reversible execution (can be compensated)
    if (!await ExecuteStepAsync(saga, "BackupContact", ...)) { await CompensateAsync(saga); return; }
    if (!await ExecuteStepAsync(saga, "AnonymizeContact", ...)) { await CompensateAsync(saga); return; }
    if (!await ExecuteStepAsync(saga, "DeactivateUser", ...)) { await CompensateAsync(saga); return; }

    // === POINT OF NO RETURN ===
    // After this, compensation is not possible

    // Phase 3: Irreversible execution
    if (!await ExecuteStepAsync(saga, "PurgePersonalData", ...))
    {
        // Cannot compensate - log for manual review
        _logger.LogCritical(
            "Saga {SagaId} failed after point of no return. Manual intervention required.",
            saga.Id);
        
        saga.MarkAsFailed("Failed after point of no return");
        await _sagaRepository.UpdateAsync(saga);
        return;
    }

    saga.MarkAsCompleted();
}
```

---

## 4. Complete Flow Example

### Front-End Flow

```
1. User clicks "Delete Account"
   ↓
2. UI calls /api/gdpr/delete-request/validate (pre-validation)
   ↓
3a. Validation fails → Show errors to user, don't proceed
3b. Validation passes → Show confirmation dialog
   ↓
4. User confirms deletion
   ↓
5. UI calls /api/gdpr/delete-request (start saga)
   ↓
6. Saga executes:
   - Runs pre-validation again (inline, if needed)
   - Executes reversible steps (with compensation support)
   - Executes irreversible steps (point of no return)
   ↓
7a. Saga completes → Success
7b. Saga fails before point of no return → Compensate and restore
7c. Saga fails after point of no return → Log for manual review
```

### Saga Steps Structure

```
┌─────────────────────────────────────┐
│     PRE-VALIDATION PHASE            │
│  IsPreValidation = true             │
│  HasCompensation = false            │
│  (Can be called from front-end)     │
├─────────────────────────────────────┤
│  ✓ ValidateLeases                   │
│  ✓ ValidateInvoices                 │
│  ✓ ValidateDataDependencies         │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   REVERSIBLE EXECUTION PHASE        │
│  IsPreValidation = false            │
│  HasCompensation = true             │
│  (Can be rolled back)               │
├─────────────────────────────────────┤
│  → BackupContact                    │
│  → AnonymizeContact                 │
│  → DeactivateUser                   │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   === POINT OF NO RETURN ===        │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│  IRREVERSIBLE EXECUTION PHASE       │
│  IsPreValidation = false            │
│  HasCompensation = false            │
│  (Cannot be rolled back)            │
├─────────────────────────────────────┤
│  → RemoveGroupMemberships           │
│  → PurgePersonalData                │
└─────────────────────────────────────┘
```

---

## 5. Summary

### Validation Features

| Feature                   | Purpose                         | Usage                                     |
|---------------------------|---------------------------------|-------------------------------------------|
| `IsPreValidation`         | Mark read-only validation steps | `new SagaStep { IsPreValidation = true }` |
| `ValidateAsync()`         | Run only validation steps       | `await orchestrator.ValidateAsync(saga)`  |
| `ValidateStepAsync()`     | Implement validation logic      | Override in orchestrator                  |
| `GetPreValidationSteps()` | Get validation steps            | `saga.GetPreValidationSteps()`            |
| Front-end endpoint        | Validate before saga starts     | `POST /api/.../validate`                  |

### Compensation Features

| Feature                         | Purpose                         | Usage                                     |
|---------------------------------|---------------------------------|-------------------------------------------|
| `HasCompensation`               | Mark if step can be compensated | `new SagaStep { HasCompensation = true }` |
| `CompensationName`              | Custom compensation action name | `CompensationName = "RestoreFromBackup"`  |
| `AutoCompensateAsync()`         | Automatic compensation helper   | Call from `CompensateAsync()`             |
| `GetStepsNeedingCompensation()` | Get compensatable steps         | `saga.GetStepsNeedingCompensation()`      |
| Point of no return              | Steps that can't be compensated | `HasCompensation = false` for final steps |

### Best Practices

✅ **Always validate before execution** - Use pre-validation steps  
✅ **Separate validation from execution** - Mark steps appropriately  
✅ **Order steps by reversibility** - Reversible first, irreversible last  
✅ **Use automatic compensation** - Unless you need custom logic  
✅ **Log compensation failures** - They may need manual intervention  
✅ **Test compensation paths** - Don't just test happy path

---

## See Also

- **Examples:** `/docs/examples/sagas/GDPRDeletionSagaV2.cs` and `GDPRDeletionOrchestratorV2.cs`
- **Quick Reference:** `/docs/QUICK-REFERENCE.md`
- **Implementation Checklist:** `/docs/IMPLEMENTATION-CHECKLIST.md`

