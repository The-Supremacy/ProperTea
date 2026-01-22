# ProperTea System Architecture

## System Overview
ProperTea is a multi-tenant Real Estate ERP built on .NET 10 using a microservices architecture.
- [cite_start]**Orchestration**: .NET Aspire (`ProperTea.AppHost`) manages local development containers (PostgreSQL, Redis, RabbitMQ, Keycloak, MailPit)[cite: 580, 582, 587, 590].
- [cite_start]**Communication**: Wolverine (CQRS + Messaging) over RabbitMQ[cite: 10, 103].
- [cite_start]**Persistence**: Marten (PostgreSQL) as an Event Store and Document DB[cite: 21, 112].
- [cite_start]**Identity**: Keycloak (External IdP) synced via API[cite: 14, 291].
- [cite_start]**Frontend Gateway**: BFF Pattern (YARP + Typed Clients)[cite: 365, 373].

## Service Boundaries

### Organization Service (`ProperTea.Organization`)
**Responsibility**: The "Tenant Master". Manages organization lifecycles, domains, and Keycloak synchronization.
- [cite_start]**Aggregate**: `OrganizationAggregate` (Implements `IRevisioned`)[cite: 248].
- **Persistence**: Event Sourcing with Marten. [cite_start]Events are defined in `OrganizationEvents.cs`[cite: 281].
- [cite_start]**Integration**: Uses `KeycloakClient` to provision organizations and users remotely[cite: 218, 290].
- [cite_start]**Messaging**: Publishes `organization.events` exchange[cite: 10].

### User Service (`ProperTea.User`)
**Responsibility**: Manages user profiles and "Last Seen" tracking.
- [cite_start]**Aggregate**: `UserProfileAggregate`[cite: 52].
- [cite_start]**Flow**: Auto-creates profile on first login via `GetMyProfile`[cite: 43, 47].
- [cite_start]**Messaging**: Listens to `organization.events` to build local read models[cite: 11].

### Landlord BFF (`ProperTea.Landlord.Bff`)
**Responsibility**: Backend-for-Frontend to secure the Frontend application.
- **Auth**: Handles OIDC (Code Flow) with Keycloak. [cite_start]Stores sessions in Redis[cite: 410, 419].
- [cite_start]**Token Forwarding**: `TokenForwardingHandler` injects the Access Token into downstream HTTP calls[cite: 425].
- **Business Logic**: None. [cite_start]Acts as a pass-through mapper only[cite: 364, 373].

## Development Patterns

### Vertical Slice Architecture
Code is organized by **Feature**, not by Layer.
- [cite_start]**Path**: `Features/{FeatureName}/` (e.g., `Features/Organizations/Lifecycle/RegisterOrganization.cs`)[cite: 209].
- **Components**: A feature file typically contains the Command, Handler, Validator, and Result types.

### Wolverine Handlers
- [cite_start]Implement `IWolverineHandler`[cite: 65, 135].
- [cite_start]Inject `IDocumentSession` (Marten) for persistence transactions[cite: 75, 135].
- [cite_start]**Side Effects**: Use `IMessageBus.PublishAsync` only for integration events or cross-boundary communication[cite: 79, 156].
- [cite_start]**Integration Events**: Must use `[MessageIdentity]` attributes in `IntegrationEvents.cs` files to ensure contract stability across services[cite: 87, 129].

### Marten Configuration
- [cite_start]**Aggregates**: Must be `internal` or `public` classes implementing `IRevisioned`[cite: 52, 248].
- [cite_start]**Events**: Defined as `public record` inside a static `Events` class[cite: 84, 281].
- [cite_start]**Tenancy**: `opts.Policies.AllDocumentsAreMultiTenanted()` is enabled globally[cite: 21, 112].

## Shared Contracts
[cite_start]The source of truth for integration models is located in `/shared/ProperTea.Contracts`[cite: 504].
- [cite_start]Interfaces define the event contracts (e.g., `IOrganizationRegistered`)[cite: 504].
- Services implement these interfaces in their specific integration event classes.
