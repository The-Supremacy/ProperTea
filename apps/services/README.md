# Backend Services

This directory contains all backend microservices for ProperTea. Each service is independently deployable, event-sourced, and multi-tenanted.

## Current Services

### [ProperTea.Organization](./ProperTea.Organization/)
The "Tenant Master" that orchestrates headless registration with ZITADEL and manages organization lifecycles. Publishes `organizations.registered.v1` that triggers tenant initialization across all services.

### [ProperTea.User](./ProperTea.User/)
Manages user profiles, preferences, and activity tracking. Subscribes to organization registration events to create initial user profiles.

### [ProperTea.Company](./ProperTea.Company/)
Manages legal business entities (Companies) that own properties. Automatically creates a default company when organizations are registered.

## Service Architecture

All services follow consistent patterns:
- **Event Sourcing**: Marten with PostgreSQL
- **Messaging**: Wolverine (CQRS) + RabbitMQ
- **Multi-Tenancy**: Marten conjoined tenancy (ZITADEL org ID as TenantId)
- **HTTP**: Wolverine.HTTP with auto-discovered endpoints
- **Authentication**: JWT Bearer (ZITADEL)

## Folder Structure Template

Each backend service should follow this structure:

```
ProperTea.{ServiceName}/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Config/                      # Service-level configuration
│   ├── AuthenticationConfig.cs
│   ├── MartenConfiguration.cs
│   ├── OpenApiConfig.cs
│   └── WolverineConfiguration.cs
├── Extensions/                  # Service-level extension methods
│   └── WolverineExtensions.cs
└── Features/                    # Feature-based organization
    └── {FeatureName}/
        ├── README.md            # Feature documentation
        ├── {Aggregate}Aggregate.cs
        ├── {Aggregate}Events.cs
        ├── {Aggregate}Endpoints.cs
        ├── {Aggregate}IntegrationEvents.cs
        ├── ErrorCodes.cs
        ├── Configuration/       # Feature-specific Marten/Wolverine config
        │   ├── {Feature}MartenConfiguration.cs
        │   └── {Feature}WolverineConfiguration.cs
        ├── Domain/              # Domain services (cross-aggregate logic)
        │   └── {Feature}DomainService.cs
        ├── Infrastructure/      # External integrations
        │   ├── I{External}Client.cs
        │   └── {External}Client.cs
        ├── Lifecycle/           # Use case handlers
        │   ├── {Command}.cs     # Command + Validator + Handler
        │   └── {Query}.cs       # Query + Handler
        ├── Policies/            # Reusable business rules
        │   └── {Policy}Policy.cs
        └── Projections/         # Read models
            ├── {Aggregate}ListView.cs
            └── {Aggregate}DetailsView.cs
```

## Design Principles

### Feature Organization
- Each feature is self-contained within its folder
- Feature owns its configuration, infrastructure, and domain logic
- Aligns with Vertical Slice Architecture and Bounded Context patterns

### Event Sourcing
- Aggregates implement `IRevisioned`
- Events are immutable records in `{Aggregate}Events.cs`
- Apply methods inside aggregate mutate state
- Decider pattern: domain methods return events

### Handlers
- Commands/Queries + Validators + Handlers in one file
- Implement `IWolverineHandler`
- Wolverine manages transactions automatically
- Use domain services for cross-aggregate validations

### Policies
- For business rules spanning multiple aggregates
- Require external context (IDocumentSession, other dependencies)
- Reusable across multiple handlers
- Example: subscription limits, access control

### Projections
- Read-optimized models projected from events
- Configured in Feature's MartenConfiguration
- Separate list views from detail views
