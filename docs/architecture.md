# System Architecture

Multi-tenant Real Estate ERP. .NET 10, microservices, event-sourced.

## Technology Stack

| Concern | Technology | Notes |
|---|---|---|
| Orchestration | .NET Aspire | `ProperTea.AppHost` manages PostgreSQL, Redis, RabbitMQ, ZITADEL, MailPit |
| CQRS + Messaging | Wolverine | Over RabbitMQ. Handlers implement `IWolverineHandler` |
| Persistence | Marten (PostgreSQL) | Event Store + Document DB. All documents multi-tenanted |
| Identity | ZITADEL | External IdP. Org ID used directly as Marten `TenantId` (ADR 0010) |
| Authorization | OpenFGA | Relationship-Based Access Control with contextual tuples (ADR 0004, 0008) |
| Frontend | Angular 21 | Zoneless, signals, Tailwind CSS 4, Transloco i18n |
| Frontend Gateway | BFF (YARP) | Pass-through only. No business logic (ADR 0003) |

## Service Map

```
                  ┌──────────────┐
                  │  Angular SPA │
                  └──────┬───────┘
                         │ OIDC
                  ┌──────┴───────┐
                  │  Landlord BFF│ ── Redis (sessions)
                  └──────┬───────┘
                         │ HTTP + X-Organization-Id header
          ┌──────────────┼──────────────┐
          │              │              │
   ┌──────┴──────┐ ┌────┴─────┐ ┌──────┴──────┐
   │ Organization│ │ Company  │ │    User     │
   │   Service   │ │ Service  │ │   Service   │
   └──────┬──────┘ └────┬─────┘ └──────┬──────┘
          │              │              │
          └──────────────┼──────────────┘
                         │ RabbitMQ (integration events)
                    ┌────┴─────┐
                    │ PostgreSQL│ (per-service databases)
                    └──────────┘
```

## Service Boundaries

### Organization Service (`ProperTea.Organization`)
The "Tenant Master". Orchestrates headless registration and manages organization lifecycles.
- Registration: Wolverine Reliable Handler calls ZITADEL v2 `AddOrganization` API for atomic Org + User creation, then persists `OrganizationAggregate`.
- Publishes: `organizations.registered.v1`, `organizations.updated.v1`.

### User Service (`ProperTea.User`)
Manages user profiles and "Last Seen" tracking.
- Aggregate: `UserProfileAggregate`.
- Subscribes to `organizations.registered.v1` to create local profile read models.

### Company Service (`ProperTea.Company`)
Manages legal business entities (Companies) that own properties.
- Aggregate: `CompanyAggregate` (Event Sourced, `ITenanted`).
- Subscribes to `organizations.registered.v1` to create a default company.
- Publishes: `companies.created.v1`, `companies.deleted.v1`.
- Endpoints: Wolverine.HTTP with `InvokeForTenantAsync`.

### Property Service (`ProperTea.Property`) -- planned
Owns the "Physical Reality": physical attributes, inventory, building structures.
Separated from commercial concerns (asset tracking without active rentals).

### Rental Service (`ProperTea.Rental`) -- planned
Owns the "Commercial Reality": internal schedules, base financials, rentable status, blocks.
Calculates "Lost Rent" (base rent vs. actual contracts).

### Work Order Service (`ProperTea.WorkOrder`) -- planned
Maintenance tasks and inspections lifecycle.
Uses cross-tenant projections for contractor visibility. OpenFGA for resource authorization.

### Landlord BFF (`ProperTea.Landlord.Bff`)
Secures frontend, forwards authenticated requests. No business logic.
- OIDC Code Flow with ZITADEL. Sessions in Redis.
- `OrganizationHeaderHandler` extracts org claim, injects `X-Organization-Id` header downstream.
- `/api/session` returns user context from JWT claims.

### Landlord Portal (`apps/portals/landlord/web`)
Angular 21 SPA. Standalone components, signals, `OnPush`, Tailwind CSS 4.
- Components: Angular Aria + Material + TanStack Table (ADR 0012).
- State: Signals for local, services with signals for shared.
- Forms: Reactive only. i18n: Transloco (en, uk).

## Key Patterns

### Vertical Slice Architecture
Code organized by **feature**, not layer. Path: `Features/{FeatureName}/`.
Each feature contains: Aggregate, Events, Handlers, Endpoints, Configuration.

### Multi-Tenancy
ZITADEL org ID = Marten `TenantId` directly (ADR 0010). No mapping layer.
- BFF extracts org claim from token, forwards as `X-Organization-Id`.
- Service extracts header via `IOrganizationIdProvider`.
- All commands dispatched via `bus.InvokeForTenantAsync(tenantId, command)`.
- Marten auto-scopes all queries to the tenant.

### Authorization (Two Layers)
1. **Organization isolation**: Marten multi-tenancy. Automatic, no code needed per query.
2. **Resource permissions**: OpenFGA checks. `ListObjects` returns authorized IDs, service filters query results. Contextual tuples for cross-tenant access (ADR 0004).

### Identity (Canonical External IDs)
- ZITADEL IDs are canonical: `string OrganizationId` / `string UserId` everywhere in APIs and events (ADR 0014).
- Internal: `Guid Id` private Marten stream key. Not exposed in APIs or integration events.
- Services read standard OAuth2 `sub` claim for user ID (not ZITADEL-specific claims) (ADR 0009).

## Shared Libraries

| Project | Purpose |
|---|---|
| `ProperTea.Contracts` | Integration event interfaces (source of truth for cross-service contracts) |
| `ProperTea.Infrastructure.Common` | Auth helpers, error handling, exceptions, pagination, OpenAPI |
| `ProperTea.ServiceDefaults` | Aspire service defaults, OpenTelemetry, resilience |

## Related Documents
- [Domain language](domain.md)
- [Integration events](event-catalog.md)
- [Architecture decisions](decisions/) (ADR 0001-0012)
