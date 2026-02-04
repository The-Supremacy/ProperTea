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

### Company Service (`ProperTea.Company`)
**Responsibility**: Manages legal business entities (Companies) that own properties and conduct business operations.
- **Aggregate**: `CompanyAggregate` (Event Sourced).
- **Multi-Tenancy**: Uses ZITADEL organization ID directly as `TenantId` for performance (no internal mapping).
- **Flow**: Listens to `organizations.registered.v1` to create a default company automatically.
- **Messaging**: Publishes `companies.created.v1` and `companies.deleted.v1` integration events.
- **Endpoints**: Wolverine.HTTP auto-discovered endpoints using `InvokeForTenantAsync` pattern.

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
- **Authorization**: Checks **OpenFGA** permissions before allowing access. Marten multi-tenancy provides org-level isolation.

### Landlord BFF (`ProperTea.Landlord.Bff`)
**Responsibility**: Secures the Frontend application and forwards authenticated requests to services.
- **Auth**: Handles OIDC Code Flow with ZITADEL. Stores sessions in Redis.
- **Token Forwarding**: Extracts user context (user_id, org_id) from token and injects as headers into downstream calls.
- **Pass-Through**: No business logic or authorization checks (handled by services).
- **Session**: Provides `/session` endpoint for Angular app to retrieve user and organization context.

### Landlord Portal (`apps/portals/landlord/web`)
**Responsibility**: Angular SPA providing the landlord user interface.
- **Stack**: Angular 21+ (standalone components, signals), Tailwind CSS
- **Components**: Headless-first (Angular Aria + Spartan UI + TanStack Table)
- **State Management**: Signals for local state, Services with Signals for shared state
- **Forms**: Reactive Forms only
- **i18n**: Transloco for internationalization
- **Dark Mode**: CSS custom properties with system preference detection

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
- **Tenant Scoping**: Services extract `org_id` from token headers and set Marten tenant per request.
- **Data Isolation**: All queries automatically scoped to organization via Marten tenancy.

### Authorization Pattern
- **Organization Isolation**: Marten multi-tenancy enforces org-level data isolation.
- **Resource Permissions**: Services check OpenFGA for fine-grained access control.
- **Pattern**: `ListObjects` from OpenFGA → Filter database query with authorized IDs.
- **Defense-in-Depth**: Services verify org_id from token matches requested resources.

## Shared Contracts
The source of truth for integration models is located in `/shared/ProperTea.Contracts`.
