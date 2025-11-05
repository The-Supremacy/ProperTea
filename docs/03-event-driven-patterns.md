# Event-Driven Patterns Guide

**Version:** 1.2.0  
**Last Updated:** October 30, 2025  
**Status:** MVP 1 Reference - Revised

---

## Table of Contents

1. [Overview](#overview)
2. [Choreography vs Orchestration](#choreography-vs-orchestration)
3. [Saga Orchestration Pattern](#saga-orchestration-pattern)
4. [Outbox Pattern](#outbox-pattern)
5. [Event Catalog](#event-catalog)
6. [Implementation Examples](#implementation-examples)

---

## Overview

ProperTea uses **event-driven architecture** to achieve loose coupling between services while maintaining data consistency.

### Key Patterns

| Pattern | Use Case | Implementation |
|---------|----------|----------------|
| **Choreography** | Independent actions, eventual consistency acceptable | Services publish events, others subscribe and react |
| **Orchestration (Saga)** | Multi-step workflow requiring coordination, validation, and potential rollback | Contact GDPR Deletion: Must validate with multiple services before proceeding |

### Implementation

- **Outbox Pattern:** Guarantees "at-least-once" delivery of integration events, even if the message broker is temporarily unavailable.
- **Custom Saga Library (`ProperTea.ProperSagas`):** A lightweight framework for building and executing orchestrated workflows.
- **Event-Driven Workers:** Each service can have a separate `Worker` project responsible for consuming and reacting to integration events.

---

## Choreography vs Orchestration

### Choreography: Event-Driven Reactions

**When to use:**
- ✅ Services react independently to events
- ✅ No central coordinator needed
- ✅ Eventual consistency is acceptable
- ✅ Easy to add new subscribers without changing publishers
- ✅ Fire-and-forget scenarios

**Implementation:** Use `ProperTea.ProperIntegrationEvents` directly (no saga)

**Example: User Registration**

```
Identity Service: Creates user → Publishes UserCreated event
  ↓
Contact Service: (Waits - no action yet)
  ↓
User logs in to org for first time (via BFF)
  ↓
BFF: Detects no contact for current org → Redirects to onboarding
  ↓
User fills profile → Contact Service creates PersonalProfile
  ↓
Contact Service: Publishes ContactCreated event
  ↓
Permission Service: Listens to ContactCreated → Assigns default groups
```

**Key characteristic:** Each service decides independently what to do when it receives an event. **No orchestrator.**

```csharp
// Identity Service - Publisher
public class UserService
{
    public async Task CreateUserAsync(CreateUserCommand cmd)
    {
        var user = new User(cmd.Email);
        await _repository.AddAsync(user);
        
        // Just publish event
        await _eventPublisher.PublishAsync(new UserCreatedEvent 
        { 
            UserId = user.Id, 
            Email = cmd.Email 
        });
    }
}

// Permission Service - Subscriber
public class ContactEventHandler
{
    public async Task Handle(ContactCreatedEvent @event)
    {
        // React independently
        await _permissionService.AssignDefaultGroupsAsync(@event.ContactId);
    }
}
```

**Another Example: Property Publication**

```
Rental Management: Creates vacancy → Publishes VacancyPeriodCreated
  ↓
Market Service: Creates listing → Publishes ListingCreated
  ↓
Search Service: Indexes in Elasticsearch
  ↓
Analytics Service: Tracks metrics
```

Each service reacts independently. If one fails, the message broker retries.

---

### Orchestration: Coordinated Saga

**When to use:**
- ✅ Multi-step workflow with dependencies between steps
- ✅ Need rollback/compensation on failures
- ✅ Multiple validation steps required before execution
- ✅ Critical business process that must be atomic
- ✅ Long-running operations that may pause/resume

**Implementation:** Use `ProperTea.ProperSagas`

**Example: Contact GDPR Deletion**

```
1. User requests data deletion
   ↓
2. Contact Service starts GDPRDeletionSaga
   - Saga persisted to database
   ↓
3. Validation Phase (parallel read-only checks)
   - Orchestrator calls LeaseService.ValidateUserDeletion() 
     → Fails if active leases exist
   - Orchestrator calls InvoiceService.ValidateUserDeletion()
     → Fails if unpaid invoices exist
   - Orchestrator calls MaintenanceService.ValidateUserDeletion()
     → OK (can anonymize requests)
   ↓
4. If ANY validation fails:
   - Saga.Status = ValidationFailed
   - Return error to user
   - No compensation needed (no data was written)
   ↓
5. Execution Phase (sequential write operations)
   - Step 1: AnonymizeContact → Contact Service
   - Step 2: DeactivateUser → Identity Service
   - Step 3: RemoveGroupMemberships → Permission Service
   - Step 4: AnonymizeMaintenanceRequests → Maintenance Service
   ↓
6. If any execution step fails:
   - Saga.Status = Compensating
   - Execute compensating actions for completed steps in reverse order
   - Example: If Step 3 fails, run:
     * ReactivateUser (compensate Step 2)
     * RestoreContact (compensate Step 1)
   ↓
7. If all steps succeed:
   - Saga.Status = Completed
```

**Key characteristic:** A central orchestrator tracks state, coordinates steps, and triggers compensation.

```csharp
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        // Validation
        if (!await ExecuteStepAsync(saga, "ValidateLeases", async () => 
        {
            if (await _leaseService.HasActiveLeasesAsync(userId))
                throw new InvalidOperationException("Active leases exist");
        }))
        {
            saga.MarkAsFailed("User has active leases");
            return; // No compensation for validation failures
        }

        // Execution
        if (!await ExecuteStepAsync(saga, "AnonymizeContact", async () =>
        {
            await _contactService.AnonymizeAsync(userId);
        }))
        {
            await CompensateAsync(saga); // Trigger rollback
            return;
        }

        // More steps...
    }

    protected override async Task CompensateAsync(GDPRDeletionSaga saga)
    {
        // Rollback completed steps in reverse order
        var completedSteps = saga.Steps
            .Where(s => s.Status == SagaStepStatus.Completed)
            .Reverse();

        foreach (var step in completedSteps)
        {
            // Undo each step
        }
    }
}
```

---

### Comparison Table

| Aspect | Choreography | Orchestration (Saga) |
|--------|--------------|----------------------|
| **Coordination** | Decentralized (events) | Centralized (orchestrator) |
| **State** | Stateless (each service tracks own state) | Stateful (saga tracks overall progress) |
| **Failure Handling** | Retry via message broker | Orchestrator triggers compensation |
| **Use Case** | Independent actions | Coordinated multi-step workflows |
| **Example** | User registration, property publication | GDPR deletion, payment processing |
| **Library** | `ProperIntegrationEvents` | `ProperSagas` |
| **Complexity** | Low | Medium-High |

---

## Saga Orchestration Pattern

**Definition:** A stateful orchestrator coordinates a multi-step workflow across services, with compensation logic for rollback on failures.

**When to use:**
- Multi-step workflows requiring strict ordering
- Need to validate preconditions before executing changes
- Critical processes that must be atomic (all-or-nothing)
- Long-running operations that may need to pause and resume

### Architecture

```
┌─────────────────────────────────────────┐
│         Saga Orchestrator               │
│  (Knows: steps, order, compensation)    │
└─────────────────────────────────────────┘
           │
           │ Calls HTTP/gRPC
           ├──────────────┬──────────────┬──────────────┐
           ▼              ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Contact  │   │ Identity │   │Permission│   │ Lease    │
    │ Service  │   │ Service  │   │ Service  │   │ Service  │
    └──────────┘   └──────────┘   └──────────┘   └──────────┘
```

### Key Characteristics

1. **Stateful:** Saga state is persisted to database, allowing resume after crash
2. **Step Tracking:** Each step has status (Pending → Running → Completed/Failed)
3. **Compensating Actions:** For every action, there's a corresponding undo
4. **Idempotent:** Steps can be safely retried
5. **Recoverable:** If orchestrator crashes, resume from last completed step

### Implementation Pattern

See `ProperTea.ProperSagas` in [04-shared-libraries.md](./04-shared-libraries.md) for detailed implementation.

**High-level flow:**

```csharp
// 1. Define saga state
public class GDPRDeletionSaga : SagaBase
{
    // Tracks steps, status, timestamps, error messages
    // Stores arbitrary data as JSON
}

// 2. Define orchestrator
public class GDPRDeletionOrchestrator : SagaOrchestratorBase<GDPRDeletionSaga>
{
    protected override async Task ExecuteStepsAsync(GDPRDeletionSaga saga)
    {
        // Execute each step
        // If step fails, CompensateAsync is called
    }
    
    protected override async Task CompensateAsync(GDPRDeletionSaga saga)
    {
        // Rollback completed steps in reverse order
    }
}

// 3. Start saga
var saga = new GDPRDeletionSaga();
saga.SetData("userId", userId);
await orchestrator.StartAsync(saga);

// 4. Resume after crash (optional)
await orchestrator.ResumeAsync(sagaId);
```

### Long-Running Sagas

For sagas that need to wait for external events (e.g., user approval, payment confirmation):

**Option 1: Local Development - Background Polling**

```csharp
public class SagaProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingSagas = await _repository.FindByStatusAsync(SagaStatus.WaitingForCallback);
            
            foreach (var sagaId in pendingSagas)
            {
                await _orchestrator.ResumeAsync(sagaId);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

**Option 2: Production - Azure Durable Functions**

Use Durable Functions for orchestration with built-in state management, timers, and retries. Saga state still persists in your database for auditing.

```csharp
[FunctionName("GDPRDeletionDurableOrchestrator")]
public async Task RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var sagaId = context.GetInput<Guid>();
    
    // Call saga orchestrator
    await context.CallActivityAsync("ExecuteSaga", sagaId);
    
    // Wait for approval (up to 7 days)
    var approval = await context.WaitForExternalEvent<bool>("approval", TimeSpan.FromDays(7));
    
    if (approval)
    {
        await context.CallActivityAsync("ResumeSaga", sagaId);
    }
}
```
