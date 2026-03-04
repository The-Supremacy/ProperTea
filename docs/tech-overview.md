# ProperTea -- Technology Showcase

A multi-tenant Real Estate ERP built as a learning project to explore modern cloud-native .NET technologies end-to-end -- from event sourcing to Kubernetes GitOps -- without paying for any external service.

---

## 1. Project Goal

Learn the full cloud-native .NET stack by building a realistic multi-tenant SaaS product: a property management platform for landlords. Every infrastructure dependency runs locally -- self-hosted identity provider, message broker, databases, observability, and a bare-metal Kubernetes cluster on KVM virtual machines. The only paid tool is GitHub Copilot.

**Domain**: Real Estate ERP (Properties, Buildings, Units, Companies, Rentals, Maintenance).
**Users**: Landlord organizations managing multiple legal entities and physical properties.

---

## 2. Architecture at a Glance

```
                  +------------------+
                  |  Angular 21 SPA  |
                  |  (Zoneless,      |
                  |   Signals,       |
                  |   Spartan UI)    |
                  +--------+---------+
                           | OIDC (Keycloak)
                  +--------+---------+
                  | Landlord BFF     |
                  | (YARP, Redis     |
                  |  sessions)       |
                  +--------+---------+
                           | HTTP + X-Organization-Id
          +----------------+----------------+
          |                |                |
   +------+------+  +-----+------+  +------+------+
   | Organization |  |  Company   |  |  Property   |
   |   Service    |  |  Service   |  |   Service   |
   +------+-------+  +-----+------+  +------+------+
          |                 |                |
          +--------+--------+--------+-------+
                   |                 |
              +----+----+    +-------+-------+
              |RabbitMQ |    | PostgreSQL    |
              |(events) |    | (per-service) |
              +---------+    +---------------+
```

Four implemented services, each owning its own Marten database. A User Service also exists (not shown for brevity). Three more services are planned (Rental, Work Order, Market).

---

## 3. .NET Aspire -- Orchestration and Local Development

.NET Aspire is the local development orchestrator. A single `dotnet run --project orchestration/ProperTea.AppHost` starts the entire stack: PostgreSQL, Redis, RabbitMQ, Keycloak, MailPit, and all four microservices.

The AppHost defines the dependency graph declaratively:

```csharp
var postgres = builder.AddPostgres("postgres", username, password)
    .WithPgAdmin(op => op.WithHostPort(54320))
    .WithDataVolume("postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var companyDb = postgres.AddDatabase("company-db");

var companyService = builder.AddProject<Projects.ProperTea_Company>("company")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithReference(companyDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak);
```

Aspire provides a dashboard (traces, logs, metrics) out of the box. Each service gets its own database. `WaitFor` ensures health checks pass before dependent services start.

---

## 4. Event Sourcing and CQRS (Marten + Wolverine)

**Marten** serves as both Event Store and Document DB on top of PostgreSQL. Aggregates are rebuilt from domain events (event sourcing) and also queryable as inline projections (document DB).

**Wolverine** provides CQRS command/query handling and messaging over RabbitMQ. Handlers implement `IWolverineHandler`.

### Aggregate -- Decider Pattern

Events are the source of truth. The aggregate exposes factory/mutation methods that return events (never mutate state directly). `Apply()` methods fold events into current state.

```csharp
// CompanyEvents.cs -- immutable event records
public static class CompanyEvents
{
    public record Created(Guid CompanyId, string Code, string Name, DateTimeOffset CreatedAt);
    public record NameUpdated(Guid CompanyId, string Name, DateTimeOffset UpdatedAt);
    public record Deleted(Guid CompanyId, DateTimeOffset DeletedAt);
}

// CompanyAggregate.cs -- decider pattern
public class CompanyAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? TenantId { get; set; }

    // Factory: returns event, not aggregate
    public static Created Create(Guid id, string code, string name, DateTimeOffset createdAt)
    {
        ValidateCode(code);
        return new Created(id, code, name, createdAt);
    }

    // Mutation: returns event, not void
    public Deleted Delete(DateTimeOffset deletedAt) => new(Id, deletedAt);

    // State folding
    public void Apply(Created e) { Id = e.CompanyId; Code = e.Code; Name = e.Name; }
    public void Apply(Deleted e) { CurrentStatus = Status.Deleted; }
}
```

### Command Handler

One command per handler file. The handler validates, calls the aggregate, starts an event stream, and publishes integration events.

```csharp
public record CreateCompany(string Code, string Name);

public class CreateCompanyHandler : IWolverineHandler
{
    public async Task<Guid> Handle(
        CreateCompany command, IDocumentSession session, IMessageBus bus)
    {
        var companyId = Guid.NewGuid();
        var created = CompanyAggregate.Create(companyId, command.Code, command.Name, DateTimeOffset.UtcNow);

        _ = session.Events.StartStream<CompanyAggregate>(companyId, created);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new CompanyCreated { CompanyId = companyId, ... });
        return companyId;
    }
}
```

### HTTP Endpoint

Wolverine.HTTP endpoints extract the tenant ID and dispatch commands with tenant scoping:

```csharp
[WolverinePost("/companies")]
[Authorize]
public static async Task<IResult> CreateCompany(
    CreateCompanyRequest request, IMessageBus bus, IOrganizationIdProvider orgProvider)
{
    var tenantId = orgProvider.GetOrganizationId()
        ?? throw new UnauthorizedAccessException("Organization ID required");

    var companyId = await bus.InvokeForTenantAsync<Guid>(tenantId, new CreateCompany(request.Code, request.Name));
    return Results.Created($"/companies/{companyId}", new { Id = companyId });
}
```

---

## 5. Multi-Tenancy

The IdP's organization ID is used directly as Marten's `TenantId`. No mapping layer, no lookup table.

**Flow**: Keycloak embeds an `organization` claim in the access token. The BFF extracts it and forwards it as an `X-Organization-Id` HTTP header. Each backend service reads the header, and all Wolverine commands are dispatched via `bus.InvokeForTenantAsync(tenantId, command)`. Marten's conjoined tenancy automatically scopes every query and event stream to that tenant.

```
Token claim: { "organization": { "<uuid>": { "name": "Acme Holdings" } } }
    --> BFF extracts UUID --> X-Organization-Id header
        --> Service reads header --> InvokeForTenantAsync(tenantId, ...)
            --> Marten auto-scopes to tenant partition
```

This achieves row-level isolation in a shared PostgreSQL instance with zero per-query filtering code.

---

## 6. Identity and Authentication (Keycloak)

Keycloak (v26+) provides authentication with its native Organizations feature for multi-tenancy. Each customer (landlord company) is a Keycloak Organization. Users belong to one organization; the org ID flows as a JWT claim.

**Key decisions**:
- Keycloak replaced ZITADEL mid-project (see Project Journal for the story).
- Token validation is split: introspection (RFC 7662) for auth-sensitive services (Organization, User), local JWT bearer for performance-sensitive services (Company, Property).
- Headless registration: the Angular SPA collects org data and calls the Organization Service, which provisions the Keycloak Organization + admin user via the Admin REST API. The login UI is Keycloak's hosted page (no custom container).
- Service accounts use standard OAuth2 client credentials. No proprietary signed JWT files.

---

## 7. Authorization -- What Exists and What's Planned

**Implemented**: Marten multi-tenancy provides automatic organization-level data isolation. Every query is scoped to the tenant. This is the first layer and covers 90% of access control needs for a single-portal product.

**Planned (deferred)**: OpenFGA (Zanzibar-based Relationship-Based Access Control) for fine-grained resource permissions. The design is documented: `ListObjects` would return authorized resource IDs, services filter results. Contextual tuples would enable cross-tenant contractor access (e.g., a contractor org viewing assigned work orders in a landlord's tenant).

OpenFGA was deferred because the project hasn't reached the Work Order / contractor features where it becomes necessary. The two-layer model (Marten isolation + OpenFGA resource checks) remains the intended architecture.

---

## 8. Messaging and Integration Events (RabbitMQ + Wolverine)

Services communicate asynchronously via integration events over RabbitMQ. Wolverine handles message routing, retries, and dead-letter queues.

**Contract project**: `shared/ProperTea.Contracts/` is the source of truth for cross-service event interfaces. Events are defined as interfaces to allow Wolverine's envelope-based polymorphic dispatch.

```csharp
// ProperTea.Contracts/Events/CompanyIntegrationEvents.cs
public interface ICompanyCreated
{
    public Guid CompanyId { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAt { get; }
}
```

**Cross-service data sync** uses a two-channel approach: fat integration events for real-time updates + HTTP snapshot endpoints for initial seeding and disaster recovery. All writes use timestamp guards for idempotency.

---

## 9. Frontend (Angular 21 + Spartan UI)

The Landlord Portal is an Angular 21 SPA using the latest patterns: zoneless change detection, signals (no RxJS for state), standalone components, and `OnPush` everywhere.

**Component library**: Spartan UI -- the Angular equivalent of shadcn/ui. "Brain" provides headless accessible primitives (npm packages). "Helm" provides Tailwind-styled component templates that are copied into the project and owned by the team. This replaces Angular Material which was abandoned mid-project due to theming conflicts with Tailwind.

**Other frontend tech**:
- TanStack Table for data grids (column definitions, sorting, pagination)
- Angular Aria for accessible headless primitives
- Transloco for i18n (English + Ukrainian, runtime language switching)
- Reactive Forms exclusively, field-level validation

**Feature structure**: Each feature is a lazy-loaded route module with models, services, list view, detail view, and optional drawers/embedded lists.

---

## 10. BFF Pattern

The Landlord BFF is a pass-through gateway with no business logic. It handles:
- OIDC Code Flow authentication with Keycloak (sessions stored in Redis)
- `OrganizationHeaderHandler`: extracts the org claim from the session, injects `X-Organization-Id` header on all downstream requests
- Reverse proxying to backend services via YARP
- A `/api/session` endpoint that returns user context from JWT claims

The BFF shields the SPA from knowing service URLs and handles the cookie-to-token translation.

---

## 11. Kubernetes and GitOps -- The Zero-Cost Infrastructure

### Cluster: Talos Linux on KVM

The Kubernetes cluster is not k3d or minikube. It's a proper 3-node Talos Linux cluster running on KVM virtual machines, nested inside a Hyper-V Ubuntu VM:

```
Windows PC (Hyper-V)
  +-- propertea-k8s-local (Ubuntu 24.04, 8 vCPU, 24 GB RAM)
       +-- talos-cp-01      (KVM, 2 vCPU, 4 GB, control plane)
       +-- talos-worker-01   (KVM, 2 vCPU, 8 GB, worker)
       +-- talos-worker-02   (KVM, 2 vCPU, 8 GB, worker)
```

Talos Linux is an immutable, API-managed Kubernetes OS. No SSH, no package manager -- all configuration via `talosctl`. Nodes are ephemeral; stateful data lives on Longhorn persistent volumes.

### Networking: Cilium

Cilium is the sole networking layer: CNI, kube-proxy replacement, L2 load balancer, and Gateway API implementation. Services are exposed via `LoadBalancer` IPs on a dedicated subnet with Cilium L2 ARP announcements. TLS is terminated at the Cilium Gateway with cert-manager self-signed certificates.

### GitOps: ArgoCD + Kustomize

The entire cluster state is declared in Git under `deploy/`:

```
deploy/
  infrastructure/
    base/                  # Shared Kustomize bases
      platform/            # ArgoCD, Envoy Gateway, Keycloak, Longhorn
      workloads/           # PgAdmin, RabbitMQ, Redis
      o11y/                # Grafana, VictoriaMetrics, Tempo, Alloy
    self-hosted/           # Bare-metal specific (Infisical, Longhorn)
  environments/
    local/                 # Local cluster overlay
      cluster/             # Talos config, bootstrap scripts
      platform/            # ArgoCD, cert-manager, Cilium Gateway, Keycloak
      workloads/           # MailPit, PgAdmin, RabbitMQ, Redis
      o11y/                # Full observability stack
      root-app.yaml        # ArgoCD App-of-Apps entry point
```

ArgoCD watches the Git repo and reconciles cluster state automatically. Infrastructure changes are PR-based.

### Observability Stack

All self-hosted, all free:

| Concern | Tool |
|---|---|
| Metrics | VictoriaMetrics (Prometheus-compatible) |
| Logs | VictoriaLogs |
| Traces | Tempo |
| Dashboards | Grafana |
| Collection | Grafana Alloy (OpenTelemetry collector) |
| K8s metrics | kube-state-metrics + node-exporter + metrics-server |

### Secrets: SOPS + age

Secrets are encrypted in Git using SOPS with age keys. ArgoCD decrypts them at sync time. No external vault service required for the local environment (Infisical is available for self-hosted scenarios).

---

## 12. AI-Assisted Development

The repository has a structured AI instruction system for GitHub Copilot:

```
.github/
  copilot-instructions.md          # Global project context
  instructions/
    dotnet.instructions.md         # Applied to all *.cs files
    angular.instructions.md        # Applied to landlord-portal/**
    bff.instructions.md            # Applied to Bff/**
    contracts.instructions.md      # Applied to ProperTea.Contracts/**
  skills/
    new-backend-feature/SKILL.md   # Step-by-step vertical slice scaffold
    new-angular-feature/SKILL.md   # Step-by-step Angular feature scaffold
    new-integration-event/SKILL.md # Cross-service event wiring
  prompts/
    code-review.prompt.md          # Code review checklist
    update-docs.prompt.md          # Documentation update workflow
```

Context-scoped instructions are automatically applied based on file glob patterns. Skills provide repeatable step-by-step procedures for common tasks. The `/docs/dev/` folder contains the reference documentation that skills point to.

---

## 13. Vertical Slice Architecture

Code is organized by **feature**, not by layer. Each feature folder contains everything: aggregate, events, handlers, endpoints, configuration.

```
Features/Companies/
  CompanyAggregate.cs
  CompanyEvents.cs
  CompanyEndpoints.cs
  CompanyIntegrationEvents.cs
  ErrorCodes.cs
  Configuration/
    CompanyFeatureExtensions.cs
    CompanyMartenConfiguration.cs
    CompanyWolverineConfiguration.cs
  Lifecycle/
    CreateCompanyHandler.cs
    UpdateCompanyHandler.cs
    DeleteCompanyHandler.cs
    ListCompaniesHandler.cs
    GetCompanyHandler.cs
```

No `Controllers/`, `Services/`, `Repositories/` folders. A developer working on Companies touches only `Features/Companies/`.

---

## 14. What's Not Done

| Item | Status | Notes |
|---|---|---|
| CI/CD pipeline | Not started | GitHub Actions to build images, push to GHCR, ArgoCD deploys. Structurally the simplest remaining piece. |
| OpenFGA authorization | Designed, not implemented | Deferred until Work Order / contractor features are built. |
| Property Service | Implemented | Properties, Buildings, Units with full CRUD. |
| Rental Service | Not started | Commercial view of units, blocks, scheduling. |
| Work Order Service | Not started | Maintenance lifecycle, cross-tenant contractor access. |
| Keycloak migration | Partially complete | AppHost, BFF, and auth config migrated. Organization client rewrite in progress. |

---

## 15. Technology Summary

| Layer | Technology | Why |
|---|---|---|
| Runtime | .NET 10 (preview) | Latest C# features, Aspire support |
| Orchestration | .NET Aspire | Single command starts everything, dashboard for free |
| CQRS + Messaging | Wolverine | Handlers, sagas, RabbitMQ transport, tenant dispatch |
| Event Store | Marten (PostgreSQL) | Event sourcing + document DB in one, conjoined multi-tenancy |
| Message Broker | RabbitMQ | Integration events between services |
| Identity | Keycloak 26+ | Organizations feature, standard OAuth2, Admin REST API |
| Authorization | Marten multi-tenancy + OpenFGA (planned) | Row-level isolation + fine-grained ReBAC |
| Frontend | Angular 21 (zoneless, signals) | Modern reactive patterns, standalone components |
| UI Components | Spartan UI (shadcn-for-Angular) | Headless + Tailwind, owned component templates |
| Data Grids | TanStack Table | Type-safe column definitions, headless |
| i18n | Transloco | Runtime language switching, single build |
| BFF Gateway | YARP | Reverse proxy, session management |
| Kubernetes | Talos Linux on KVM | Immutable OS, API-managed, bare-metal |
| CNI + Gateway | Cilium | kube-proxy replacement, L2 LB, Gateway API |
| GitOps | ArgoCD + Kustomize | Declarative cluster state in Git |
| Storage | Longhorn | Distributed block storage for PVs |
| Observability | VictoriaMetrics + Tempo + Grafana + Alloy | Full metrics/logs/traces stack |
| Secrets | SOPS + age | Encrypted secrets in Git |
| AI Tooling | GitHub Copilot | Context scoped instructions, skills, prompts |
