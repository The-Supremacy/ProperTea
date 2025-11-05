# Durable Functions Migration Guide

**Last Updated:** October 31, 2025

---

## Should You Use Activity Pattern Now for Future Durable Functions?

### ❌ No - Activity Pattern Doesn't Help Much

**Reasons:**

1. **Different Activity Models**
    - Your activities: Take saga objects, modify saga state
    - Durable activities: Take primitives, return primitives
    - Not compatible structures

2. **Deterministic Replay Requirements**
    - Durable Functions replays orchestrator code
    - No I/O in orchestrator (must use activities)
    - Different programming model

3. **State Management Differences**
    - Your sagas: State in database
    - Durable Functions: State in Azure Storage
    - Duplicate state stores

4. **Added Complexity Now**
    - Activity pattern is more complex
    - Doesn't provide value until you actually use Durable Functions
    - Premature optimization

---

## ✅ Better Approach: Design for Portability

Instead of using Activity pattern, follow these principles to make future migration easier:

### Principle 1: Keep Saga State Simple

**✅ Do:**

```csharp
// Simple, serializable primitives
saga.SetData("userId", Guid.Parse("..."));
saga.SetData("backupId", "backup-123");
saga.SetData("amount", 100.50m);
```

**❌ Don't:**

```csharp
// Complex objects, service dependencies
saga.SetData("user", new User { ... }); // Entire user object
saga.SetData("service", _leaseService); // Service instance
```

### Principle 2: Extract Business Logic to Services

**✅ Do:**

```csharp
// Business logic in reusable services
public interface IGDPRValidationService
{
    Task<bool> ValidateLeasesAsync(Guid userId);
    Task<bool> ValidateInvoicesAsync(Guid userId);
}

// Orchestrator just coordinates
protected override async Task ExecuteStepsAsync(MySaga saga)
{
    var isValid = await _validationService.ValidateLeasesAsync(saga.GetUserId());
    if (!isValid) throw new Exception();
}
```

**❌ Don't:**

```csharp
// Business logic embedded in orchestrator
protected override async Task ExecuteStepsAsync(MySaga saga)
{
    var leases = await _context.Leases.Where(l => l.UserId == saga.GetUserId()).ToListAsync();
    var activeLeases = leases.Where(l => l.Status == LeaseStatus.Active);
    if (activeLeases.Any())
    {
        var overdueLeases = activeLeases.Where(l => l.EndDate < DateTime.UtcNow);
        // ... more complex logic ...
    }
}
```

### Principle 3: Keep Steps Small and Focused

**✅ Do:**

```csharp
// Small, single-purpose steps
await ExecuteStepAsync(saga, "ValidateLeases", async () => { ... });
await ExecuteStepAsync(saga, "CreateBackup", async () => { ... });
await ExecuteStepAsync(saga, "AnonymizeContact", async () => { ... });
```

**❌ Don't:**

```csharp
// Large, multi-purpose steps
await ExecuteStepAsync(saga, "ProcessEverything", async () =>
{
    // Validation
    // Backup
    // Execution
    // Notification
    // All in one giant step
});
```

### Principle 4: Use Interfaces, Not Concrete Dependencies

**✅ Do:**

```csharp
public class MyOrchestrator : SagaOrchestratorBase<MySaga>
{
    private readonly ILeaseService _leaseService;      // Interface
    private readonly IContactService _contactService;  // Interface
    
    // Services can be swapped with Durable Functions activities later
}
```

**❌ Don't:**

```csharp
public class MyOrchestrator : SagaOrchestratorBase<MySaga>
{
    private readonly DbContext _context;              // Direct DB access
    private readonly HttpClient _httpClient;          // Direct HTTP
    
    // Hard to replace with Durable Functions activities
}
```

---

## Migration Path to Durable Functions

### Step 1: Your Current Saga (Portable Design)

```csharp
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    private readonly IGDPRValidationService _validationService;
    private readonly IGDPRExecutionService _executionService;
    
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        // Thin orchestration, services do the work
        var isValid = await _validationService.ValidateLeasesAsync(saga.GetUserId());
        var backupId = await _executionService.CreateBackupAsync(saga.GetUserId());
    }
}
```

### Step 2: Create Durable Functions Orchestrator

```csharp
[FunctionName("GDPRDeletionOrchestrator")]
public async Task<string> RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var input = context.GetInput<GDPRDeletionInput>();
    
    // Same flow, but using activities
    var isValid = await context.CallActivityAsync<bool>("ValidateLeases", input.UserId);
    var backupId = await context.CallActivityAsync<string>("CreateBackup", input.UserId);
    
    return "Completed";
}
```

### Step 3: Wrap Services as Activities

```csharp
// Activities are thin wrappers around existing services
[FunctionName("ValidateLeases")]
public async Task<bool> ValidateLeases(
    [ActivityTrigger] Guid userId,
    [Inject] IGDPRValidationService validationService)  // Same service!
{
    return await validationService.ValidateLeasesAsync(userId);
}

[FunctionName("CreateBackup")]
public async Task<string> CreateBackup(
    [ActivityTrigger] Guid userId,
    [Inject] IGDPRExecutionService executionService)    // Same service!
{
    return await executionService.CreateBackupAsync(userId);
}
```

### Step 4: Keep Both Implementations

```
Local Development:
  → Use ProperSagas orchestrator
  → Direct service calls
  → Easier to debug

Production:
  → Use Durable Functions orchestrator
  → Activities wrap services
  → Better scalability
```

---

## What Gets Reused vs Rewritten

### ✅ Reused (No Changes Needed)

- **Service implementations** - Business logic stays the same
- **Service interfaces** - Contracts remain unchanged
- **Domain models** - Entities, value objects
- **Database schema** - No changes needed
- **Validation logic** - Same rules apply
- **Compensation logic** - Same undo operations

### 🔄 Rewritten (Different Structure)

- **Orchestrator** - Durable Functions has different API
- **Step execution** - Activities instead of direct calls
- **State management** - Durable Functions state vs saga database
- **Error handling** - Durable Functions retry policies
- **Long-running** - Durable Functions timers instead of polling

---

## Comparison

### Using Activity Pattern Now

**Pros:**

- Slightly more reusable step logic

**Cons:**

- ❌ More complex now (when you don't need it)
- ❌ Doesn't actually help with Durable Functions migration
- ❌ Still need to rewrite orchestrator for Durable Functions
- ❌ Different activity models (saga objects vs primitives)

### Using Portable Design Now

**Pros:**

- ✅ Simple and easy to understand now
- ✅ Service logic fully reusable in Durable Functions
- ✅ No premature optimization
- ✅ Easy to debug and maintain

**Cons:**

- Still need to rewrite orchestrator for Durable Functions (but that's unavoidable)

---

## Recommendation

### ✅ Use Current Pattern + Portable Design Principles

**Do this:**

1. Keep saga orchestrator simple (current pattern)
2. Extract business logic to services (with interfaces)
3. Keep saga state simple (primitives only)
4. Make steps small and focused

**Don't do this:**

1. Don't adopt Activity pattern prematurely
2. Don't embed business logic in orchestrator
3. Don't store complex objects in saga state

### When to Migrate to Durable Functions

**Consider Durable Functions when:**

- ✅ Workflows run for days/weeks (timers)
- ✅ Need external event triggers
- ✅ Need automatic retry/timeout per step
- ✅ Running in Azure already
- ✅ Need very high scale

**Stick with ProperSagas when:**

- ✅ Workflows complete in seconds/minutes
- ✅ Local development is primary
- ✅ Want simple debugging
- ✅ Team prefers straightforward patterns
- ✅ Not running in Azure

---

## Migration Effort Comparison

### With Activity Pattern Now:

```
Services:           ✅ Reused (0% rewrite)
Activity wrappers:  🔄 Still need to create (100% new)
Orchestrator:       🔄 Rewrite for Durable Functions (100% rewrite)
State management:   🔄 Different model (100% rewrite)

Total reuse: ~25%
```

### With Portable Design:

```
Services:           ✅ Reused (0% rewrite)
Activity wrappers:  🔄 Still need to create (100% new)
Orchestrator:       🔄 Rewrite for Durable Functions (100% rewrite)
State management:   🔄 Different model (100% rewrite)

Total reuse: ~25%
```

**Activity pattern doesn't significantly reduce migration effort!**

---

## Example: Side-by-Side Comparison

### Portable ProperSagas Design

```csharp
// Service (reusable)
public class GDPRExecutionService : IGDPRExecutionService
{
    public async Task<string> CreateBackupAsync(Guid userId)
    {
        // Business logic here
        return backupId;
    }
}

// Orchestrator (ProperSagas)
public class GDPROrchestrator : SagaOrchestratorBase<GDPRSaga>
{
    protected override async Task ExecuteStepsAsync(GDPRSaga saga)
    {
        var backupId = await _executionService.CreateBackupAsync(saga.GetUserId());
        saga.SetBackupId(backupId);
    }
}
```

### Durable Functions Version

```csharp
// Service (SAME - reused!)
public class GDPRExecutionService : IGDPRExecutionService
{
    public async Task<string> CreateBackupAsync(Guid userId)
    {
        // Business logic here - UNCHANGED
        return backupId;
    }
}

// Activity wrapper (NEW - thin wrapper)
[FunctionName("CreateBackup")]
public async Task<string> CreateBackup(
    [ActivityTrigger] Guid userId,
    [Inject] IGDPRExecutionService service)
{
    return await service.CreateBackupAsync(userId);
}

// Orchestrator (REWRITTEN - Durable Functions API)
[FunctionName("GDPROrchestrator")]
public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext ctx)
{
    var userId = ctx.GetInput<Guid>();
    var backupId = await ctx.CallActivityAsync<string>("CreateBackup", userId);
}
```

**Notice:** The service is identical. Only the orchestrator wrapper changes.

---

## Conclusion

### Don't Use Activity Pattern Now

❌ **Activity pattern doesn't help with Durable Functions migration**

- Different activity models
- Orchestrator still needs rewrite
- Adds unnecessary complexity now

### Do Use Portable Design

✅ **Follow portable design principles:**

- Extract business logic to services
- Keep saga state simple
- Use interfaces, not concrete dependencies
- Make steps small and focused

✅ **Benefits:**

- Services are fully reusable in Durable Functions
- Simple and maintainable now
- Easy migration path when needed
- No premature optimization

### Best Practice

**Build for today's needs, with tomorrow's migration in mind:**

1. Use current ProperSagas pattern (simple)
2. Extract business logic to services (reusable)
3. Keep saga state simple (portable)
4. When you need Durable Functions, wrap services as activities (thin wrapper layer)

**This gives you the best of both worlds!**

---

## See Also

- **Portable design example:** `/docs/examples/sagas/PortableDesign.cs`
- **Durable Functions docs:** https://docs.microsoft.com/azure/azure-functions/durable/
- **Current pattern:** `/docs/examples/sagas/GDPRDeletionOrchestratorV2.cs`

