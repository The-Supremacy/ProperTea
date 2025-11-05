# Saga Design Patterns: Definition vs Implementation

**Last Updated:** October 31, 2025

---

## The Core Question

**Should saga steps be:**
1. **Pure definitions** (data structures describing what happens) + separate orchestrator implementation?
2. **Self-contained activities** (each step knows how to execute itself)?
3. **Hybrid** (support both approaches)?

---

## Pattern 1: Separation of Concerns (Current)

### Structure

```
Saga (Definition)
  ├─ Steps (List<SagaStep>)  ← What steps exist
  ├─ Data (Strongly-typed)   ← What data we work with
  └─ Helper methods          ← Getters/setters for data

Orchestrator (Implementation)
  ├─ ValidateStepAsync()     ← How to validate
  ├─ ExecuteStepsAsync()     ← How to execute
  └─ CompensateAsync()       ← How to compensate
```

### Example

```csharp
// Saga: Pure data structure
public class GDPRDeletionSaga : SagaBase
{
    public GDPRDeletionSaga()
    {
        Steps = new List<SagaStep>
        {
            new() { Name = "ValidateLeases", IsPreValidation = true },
            new() { Name = "AnonymizeContact", HasCompensation = true }
        };
    }
    
    // Just data accessors
    public Guid GetUserId() => GetData<Guid>("userId");
}

// Orchestrator: All logic here
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        // Implementation lives here
        if (!await ExecuteStepAsync(saga, "ValidateLeases", async () =>
        {
            var hasLeases = await _leaseService.HasActiveLeasesAsync(saga.GetUserId());
            if (hasLeases) throw new InvalidOperationException("Cannot delete");
        })) return;
        
        if (!await ExecuteStepAsync(saga, "AnonymizeContact", async () =>
        {
            var id = await _contactService.AnonymizeAsync(saga.GetUserId());
            saga.SetContactId(id);
        })) return;
    }
}
```

### Pros ✅

- **Simple and straightforward** - Easy to understand
- **All logic in one place** - Easy to see full flow
- **No additional abstractions** - Less cognitive overhead
- **Easy to customize** - Full control over execution
- **Good for simple to medium workflows** (3-10 steps)
- **Easy to debug** - Linear flow through orchestrator

### Cons ❌

- **Logic not reusable** - Can't share steps between sagas
- **Large orchestrators** - Can get unwieldy with many steps
- **Hard to test individual steps** - Must test whole orchestrator
- **Tight coupling** - Step logic tied to specific orchestrator

### When to Use

✅ **GDPR Deletion** - Steps are specific to this workflow  
✅ **User Onboarding** - Linear flow, saga-specific logic  
✅ **Order Cancellation** - Simple compensation logic  
✅ **Invoice Processing** - Straightforward validation and execution  

---

## Pattern 2: Activity Pattern (Durable Functions Style)

### Structure

```
Saga (Orchestrator)
  └─ Calls activities in sequence

Activities (Self-contained)
  ├─ ValidateLeasesActivity
  │   ├─ ExecuteAsync()
  │   └─ CompensateAsync()
  ├─ AnonymizeContactActivity
  │   ├─ ExecuteAsync()
  │   └─ CompensateAsync()
  └─ ...
```

### Example

```csharp
// Each step is a separate activity
public class ValidateLeasesActivity : ISagaStepAction<GDPRDeletionSaga>
{
    private readonly ILeaseService _leaseService;
    
    public async Task ExecuteAsync(GDPRDeletionSaga saga, CancellationToken ct)
    {
        var hasLeases = await _leaseService.HasActiveLeasesAsync(saga.GetUserId());
        if (hasLeases)
            throw new InvalidOperationException("Cannot delete user with active leases");
    }
    
    public async Task CompensateAsync(GDPRDeletionSaga saga, CancellationToken ct)
    {
        // No compensation needed for validation
    }
}

// Orchestrator just calls activities
public class GDPRDeletionOrchestrator
{
    public async Task ExecuteAsync(GDPRDeletionSaga saga)
    {
        await _activityExecutor.RunAsync<ValidateLeasesActivity>(saga);
        await _activityExecutor.RunAsync<AnonymizeContactActivity>(saga);
    }
}
```

### Pros ✅

- **Highly reusable** - Activities used across multiple sagas
- **Easy to test** - Test each activity independently
- **Clear responsibilities** - Each activity is self-contained
- **Good for complex workflows** - Each activity is manageable
- **Framework support** - Works well with Durable Functions
- **Automatic retries** - Framework can handle per-activity retries

### Cons ❌

- **More complexity** - More files, more abstractions
- **Harder to see full flow** - Logic scattered across activities
- **More boilerplate** - Each activity needs its own class
- **DI complexity** - Each activity needs dependency injection
- **Overkill for simple sagas** - Overhead not worth it for 3-5 steps

### When to Use

✅ **E-commerce Order Processing** - Many steps reused across order/refund/exchange sagas  
✅ **Payment Processing** - PaymentActivity used in multiple workflows  
✅ **Notification Systems** - SendEmailActivity, SendSMSActivity reused everywhere  
✅ **Complex Approval Workflows** - Multiple validation activities shared  

---

## Pattern 3: Hybrid Approach (Flexible)

### Structure

```
Orchestrator
  ├─ Supports reusable activities (optional)
  └─ Supports inline implementation (fallback)

Step Actions (Optional)
  ├─ ValidateLeasesAction (reusable)
  ├─ AnonymizeContactAction (reusable)
  └─ ... (register what you need)
```

### Example

```csharp
// Reusable action (if needed)
public class ValidateLeasesAction : ISagaStepAction<GDPRDeletionSaga>
{
    public async Task ExecuteAsync(GDPRDeletionSaga saga, CancellationToken ct)
    {
        // Reusable validation logic
    }
}

// Orchestrator uses actions where they exist, inline otherwise
public class GDPRDeletionOrchestrator
{
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        // Try to use registered action
        if (_stepActions.TryGetValue("ValidateLeases", out var actionType))
        {
            var action = _serviceProvider.GetRequiredService(actionType);
            await action.ExecuteAsync(saga);
        }
        else
        {
            // Fall back to inline implementation
            await ExecuteStepAsync(saga, "ValidateLeases", async () =>
            {
                // Inline logic here
            });
        }
    }
}
```

### Pros ✅

- **Flexible** - Use activities where beneficial, inline elsewhere
- **Gradual adoption** - Start simple, extract activities as needed
- **Best of both worlds** - Reusability + simplicity
- **Easy migration** - Extract inline logic to activities over time
- **Testable** - Test activities or orchestrator as needed

### Cons ❌

- **More complex** - Need to understand both patterns
- **Inconsistent** - Some steps inline, some activities
- **Setup overhead** - Need registration mechanism

### When to Use

✅ **Growing projects** - Start simple, extract activities as needed  
✅ **Mixed complexity** - Some steps common, others saga-specific  
✅ **Team learning** - Gradual introduction to activity pattern  

---

## Comparison Table

| Aspect | Separation | Activity Pattern | Hybrid |
|--------|-----------|------------------|--------|
| **Complexity** | Low | High | Medium |
| **Reusability** | None | High | Medium |
| **Testability** | Orchestrator-level | Activity-level | Both |
| **Learning Curve** | Easy | Steep | Medium |
| **Boilerplate** | Low | High | Medium |
| **Best For** | Simple sagas | Complex/reusable | Growing projects |
| **Files per saga** | 2-3 | 5-15+ | 3-8 |
| **DI complexity** | Low | High | Medium |
| **Debugging** | Easy | Harder | Medium |

---

## Real-World Patterns

### Microsoft Durable Functions
- **Uses:** Activity Pattern
- **Why:** Designed for complex, long-running workflows with many reusable steps
- **Trade-off:** More complexity for better scalability

### Spring State Machine
- **Uses:** Hybrid (can do both)
- **Why:** Flexible to support different use cases
- **Trade-off:** More concepts to learn

### Netflix Conductor
- **Uses:** Activity Pattern (called "Tasks")
- **Why:** Workflows as JSON configs, tasks are reusable microservices
- **Trade-off:** Requires infrastructure

### ProperSagas (Current)
- **Uses:** Separation Pattern
- **Why:** Simplicity for educational purposes and small/medium sagas
- **Trade-off:** Less reusability, but easier to understand

---

## Recommendation for ProperTea

### Keep Current Pattern as Default ✅

**Reasons:**
1. **Educational value** - Easier to understand and teach
2. **Most sagas are simple** - GDPR deletion, user onboarding, etc.
3. **Less boilerplate** - Faster to implement
4. **Debugging** - Easier to follow flow

### Add Hybrid Support (Optional) ✅

**For teams that need it:**
- Provide `ISagaStepAction<TSaga>` interface
- Show example in documentation
- Make it opt-in, not required

### Don't Force Activity Pattern ❌

**Because:**
- Overkill for most use cases in ProperTea
- Adds complexity without clear benefit
- Better patterns exist if you need that complexity (use Durable Functions)

---

## Migration Path

### Current → Hybrid (if needed)

```csharp
// Step 1: Extract reusable steps to actions
public class ValidateLeasesAction : ISagaStepAction<GDPRDeletionSaga>
{
    // Move logic from orchestrator here
}

// Step 2: Register actions
services.AddScoped<ValidateLeasesAction>();

// Step 3: Update orchestrator to use actions (optional)
if (_stepActions.TryGetValue(stepName, out var actionType))
{
    var action = _serviceProvider.GetRequiredService(actionType);
    await action.ExecuteAsync(saga);
}
else
{
    // Keep inline implementation
}
```

---

## Conclusion

### For ProperTea:

✅ **Keep current separation pattern as default**
- Simple, clear, easy to understand
- Perfect for 80% of use cases

✅ **Document hybrid approach as advanced pattern**
- For teams that need reusability
- Show in examples, make it opt-in

❌ **Don't force activity pattern**
- Too complex for typical sagas
- If you need that, use Azure Durable Functions

### Key Insight:

**The best pattern depends on:**
- Saga complexity (simple vs. complex)
- Reusability needs (saga-specific vs. shared steps)
- Team experience (beginners vs. experts)
- Infrastructure (local vs. cloud)

**For most ProperTea sagas:**
→ Current pattern (separation) is the right choice
→ Extract to activities only when you see duplication
→ Don't over-engineer

---

## See Also

- Current pattern: `/docs/examples/sagas/GDPRDeletionOrchestratorV2.cs`
- Hybrid pattern: `/docs/examples/sagas/HybridApproach.cs`
- Azure Durable Functions: https://docs.microsoft.com/azure/azure-functions/durable/

