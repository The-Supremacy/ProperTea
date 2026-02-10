---
name: new-backend-feature
description: Scaffold a new backend feature as a vertical slice in a ProperTea microservice. Use when asked to create a new aggregate, feature, domain entity, or service endpoint.
---

# Scaffold a New Backend Feature (Vertical Slice)

You are scaffolding a new feature for a ProperTea backend service.

## Before You Start
1. Read `/docs/architecture.md` to understand service boundaries.
2. Read `/docs/domain.md` to use correct ubiquitous language.
3. Identify which service owns this feature. Do NOT create cross-boundary logic.

## File Structure to Generate

Create all files under `Features/{FeatureName}/` in the target service:

```
Features/{FeatureName}/
├── {Name}Aggregate.cs            # Event-sourced aggregate (Decider pattern)
├── {Name}Events.cs               # Static class with immutable event records
├── {Name}Endpoints.cs            # Wolverine HTTP endpoints
├── {Name}IntegrationEvents.cs    # Only if cross-service events needed
├── ErrorCodes.cs                 # Static error code constants
├── Configuration/
│   ├── {Name}FeatureExtensions.cs
│   ├── {Name}MartenConfiguration.cs
│   └── {Name}WolverineConfiguration.cs
└── Lifecycle/
    ├── Create{Name}Handler.cs
    ├── Get{Name}Handler.cs
    ├── List{Name}sHandler.cs
    └── (additional command/query handlers)
```

## Aggregate Pattern

```csharp
using Marten.Metadata;
using static {ServiceNamespace}.Features.{FeatureName}.{Name}Events;

public class {Name}Aggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    // Static factory for creation (returns event, not the aggregate)
    public static Created Create(Guid id, ...) { /* validate, return event */ }

    // Instance methods for mutations (return events)
    public Deleted Delete(...) { /* validate state, return event */ }

    // Apply methods (mutate state from events)
    public void Apply(Created e) { /* set properties */ }
    public void Apply(Deleted e) { /* set status */ }
}
```

## Handler Pattern

```csharp
public record Create{Name}(string Name);

public class Create{Name}Handler : IWolverineHandler
{
    public async Task<Guid> Handle(Create{Name} command, IDocumentSession session)
    {
        var id = Guid.NewGuid();
        var created = {Name}Aggregate.Create(id, command.Name, DateTimeOffset.UtcNow);
        _ = session.Events.StartStream<{Name}Aggregate>(id, created);
        await session.SaveChangesAsync(); // Only when returning new ID
        return id;
    }
}
```

## Endpoint Pattern

```csharp
[WolverinePost("/{resource}")]
[Authorize]
public static async Task<IResult> Create{Name}(
    Create{Name}Request request,
    IMessageBus bus,
    IOrganizationIdProvider orgProvider)
{
    var tenantId = orgProvider.GetOrganizationId()
        ?? throw new UnauthorizedAccessException("Organization ID required");
    var id = await bus.InvokeForTenantAsync<Guid>(tenantId, new Create{Name}(request.Name));
    return Results.Created($"/{resource}/{id}", new { Id = id });
}
```

## Checklist
- [ ] Aggregate implements `IRevisioned` and `ITenanted`
- [ ] Events are immutable records in a static class
- [ ] Handlers implement `IWolverineHandler`
- [ ] No manual `SaveChangesAsync` unless read-after-write needed
- [ ] Error codes defined in static class with SCREAMING_SNAKE_CASE
- [ ] Endpoints use `InvokeForTenantAsync` for multi-tenancy
- [ ] Marten and Wolverine configuration registered
- [ ] Feature extension method created and called from `Program.cs`
