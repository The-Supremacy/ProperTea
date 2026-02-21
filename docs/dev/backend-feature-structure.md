# Backend Feature Structure

Reference for how vertical slice features are organized in backend services.

## Directory Layout

```
Features/{FeatureName}/
├── {Name}Aggregate.cs              # Event-sourced aggregate
├── {Name}Events.cs                 # Domain events (immutable records)
├── {Name}Endpoints.cs              # Wolverine HTTP endpoints
├── {Name}IntegrationEvents.cs      # Cross-service events (if needed)
├── ErrorCodes.cs                   # Error code constants
├── Configuration/
│   ├── {Name}FeatureExtensions.cs  # IServiceCollection extension
│   ├── {Name}MartenConfiguration.cs
│   └── {Name}WolverineConfiguration.cs
├── Lifecycle/                      # One handler per file
│   ├── Create{Name}Handler.cs
│   ├── Delete{Name}Handler.cs
│   ├── Get{Name}Handler.cs
│   ├── Get{Name}AuditLogHandler.cs
│   ├── List{Name}sHandler.cs
│   ├── Select{Name}sHandler.cs    # Lightweight list for dropdowns
│   └── Update{Name}Handler.cs
├── Domain/                         # Optional: value objects, domain services
├── Projections/                    # Optional: Marten projections
└── Policies/                       # Optional: policies, sagas
```

## Key Conventions

**Aggregates**: Decider pattern. Static factory for creation, instance methods for mutations. Both return events, never mutate state directly. `Apply()` methods handle state changes from events.

**Handlers**: One command/query per handler file. Command DTO defined as a `record` at the top of the file. Handler class implements `IWolverineHandler`.

**Endpoints**: Static class, one method per route. Extract tenant ID via `IOrganizationIdProvider`, dispatch via `bus.InvokeForTenantAsync()`.

**Events**: Immutable `record` types in a static `{Name}Events` class. Imported via `using static`.

**Error codes**: `SCREAMING_SNAKE_CASE` in a static class. Used in typed exceptions.

## Reference Implementation

See `ProperTea.Company/Features/Companies/` for a simple single-entity example, or `ProperTea.Property/Features/` for a multi-aggregate service (Properties, Buildings, Units) with integration events and cross-aggregate operations.
