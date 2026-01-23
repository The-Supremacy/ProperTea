# ProperTea System Architecture

## System Overview
ProperTea is a multi-tenant Real Estate ERP built on .NET 10 using a microservices architecture.
- **Orchestration**: .NET Aspire (`ProperTea.AppHost`) manages local development containers (PostgreSQL, Redis, RabbitMQ, ZITADEL, MailPit).
- **Communication**: Wolverine (CQRS + Messaging) over RabbitMQ.
- **Persistence**: Marten (PostgreSQL) as an Event Store and Document DB.
- **Identity**: ZITADEL (External IdP) used for authentication and organization management.
- **Authorization**: OpenFGA (Relationship-Based Access Control) using contextual tuples for fine-grained resource permissions.
- **Frontend Gateway**: BFF Pattern (YARP + Typed Clients).

## Service Boundaries

### Organization Service (`ProperTea.Organization`)
**Responsibility**: The "Tenant Master". Orchestrates headless registration and manages organization lifecycles.
- **Registration Flow**: Uses a **Reliable Handler** to call ZITADEL v2 APIs (atomic Org + User creation) and persists the local `OrganizationAggregate`.
- **Persistence**: Event Sourcing with Marten.
- **Messaging**: Publishes `organizations.registered.v1` and other lifecycle events.

### User Service (`ProperTea.User`)
**Responsibility**: Manages user profiles and "Last Seen" tracking.
- **Aggregate**: `UserProfileAggregate`.
- **Flow**: Listens to `organizations.registered.v1` to build local profile read models asynchronously.

### Property Service (`ProperTea.Property`)
**Responsibility**: Owns the "Physical Reality" of the estate.
- **Data**: Manages physical attributes, inventory, and building structures.
- **Isolation**: Separated from commercial concerns to allow asset tracking without active rentals.

### Rental Service (`ProperTea.Rental`)
**Responsibility**: Owns the "Commercial Reality" of the estate.
- **Logic**: Manages internal schedules, base financials, rentable status, and blocks (e.g., renovations).
- **Calculations**: Determines "Lost Rent" based on base rent vs. actual contracts.

### Work Order Service (`ProperTea.WorkOrder`)
**Responsibility**: Manages the lifecycle of maintenance tasks and inspections.
- **Visibility**: Uses cross-tenant projections to allow contractor organizations to view assigned tasks.
- **Authorization**: Implements **OpenFGA Contextual Tuples** to verify contractor access based on the `ExecutorOrganizationId` stored in the database.

### Landlord BFF (`ProperTea.Landlord.Bff`)
**Responsibility**: Secures the Frontend application and provides session context.
- **Auth**: Handles OIDC Code Flow with ZITADEL. Stores sessions in Redis.
- **Session Metadata**: Exposes a `/session` endpoint for the Angular app to retrieve branding (Logo URL, Primary Color) and current organization context.
- **Token Forwarding**: Injects Access Tokens into downstream calls.

## Development Patterns

### Vertical Slice Architecture
Code is organized by **Feature**, not by Layer.
- **Path**: `Features/{FeatureName}/`.

### Wolverine Handlers
- Implement `IWolverineHandler`.
- Inject `IDocumentSession` (Marten) for persistence transactions.
- **Side Effects**: Use `IMessageBus.PublishAsync` only for integration events.

### Marten Configuration
- **Aggregates**: Implement `IRevisioned`.
- **Tenancy**: `opts.Policies.AllDocumentsAreMultiTenanted()` enabled globally.

## Shared Contracts
The source of truth for integration models is located in `/shared/ProperTea.Contracts`.
