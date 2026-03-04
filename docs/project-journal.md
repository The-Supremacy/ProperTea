# ProperTea -- Project Journal

A chronological record of the major design decisions, pivots, and lessons learned over six months of building ProperTea. This document distills 18 Architecture Decision Records and one migration plan into a narrative that explains the "why" behind the codebase as it exists today.

---

## The Starting Point

The project started with a clear constraint: learn cloud-native .NET without paying for anything. This meant self-hosting everything -- identity provider, message broker, databases, Kubernetes cluster, observability. The domain (Real Estate ERP) was chosen because it's complex enough to justify microservices, event sourcing, and multi-tenancy, but familiar enough that the domain modeling wouldn't consume all the learning time.

The initial tech stack: .NET with Aspire, Marten (event sourcing on PostgreSQL), Wolverine (CQRS + messaging), ZITADEL (identity), Angular (frontend), and a Kubernetes cluster to deploy it all.

---

## Aggregate Design: Property, Building, and Unit

**The question**: How do you model the physical reality of real estate?

The first instinct was to nest Units inside Property as child entities. A property complex might have hundreds of units, and loading the entire aggregate for a single unit update would be a performance disaster. So Property and Unit became separate Aggregate Roots from day one, connected by a `PropertyId` reference.

The edge case that tested this: a standalone house. It's modeled as one Property with exactly one Unit. The patterns compose.

Later, Building went through the same extraction. It started as a child entity of Property, but write contention (updating one building locked the whole property), audit log pollution (building changes showed as property changes), and the need for independent list views all pointed the same way. Building became its own Aggregate Root with a `PropertyId` and optional `Entrance` value objects.

The Unit type system (`UnitCategory`) encodes physical constraints: an Apartment requires a `BuildingId`, a House must not have one, Commercial and Parking optionally reference a Building. This captures real-world invariants in the type system rather than in validation rules.

---

## Multi-Tenancy: From Mapping Layer to Direct ID

**The question**: How does tenant isolation work when your identity provider manages organizations?

The initial approach was textbook: the IdP has its own organization ID, we have an internal `OrganizationId`, and there's a mapping table between them. Every request would: extract the IdP org ID from the token, look up the internal ID, use that for data scoping.

This added a database lookup to every single request. For a multi-tenant SaaS where literally every operation is tenant-scoped, that's a lot of overhead for no functional benefit. The IdP org ID is already unique and stable.

The pivot: use the IdP's organization ID directly as Marten's `TenantId`. No mapping layer, no lookup table. The BFF extracts the org claim from the JWT, forwards it as an `X-Organization-Id` header, and the service dispatches all commands via `bus.InvokeForTenantAsync(tenantId, command)`. Marten's conjoined tenancy handles the rest.

This simplified the codebase significantly and eliminated a class of bugs (stale cache, mapping table out of sync). The trade-off is coupling to the IdP's ID format -- but we accepted that since switching IdPs is a major migration anyway (and we proved it when we did switch).

---

## The ZITADEL Journey

**The question**: Which identity provider gives you multi-tenancy, self-hosting, and a good developer experience?

ZITADEL was chosen initially for compelling reasons:
- Native multi-organization support (each customer gets a ZITADEL Organization)
- Org-scoped tokens (the org ID is embedded in the JWT automatically)
- gRPC API for programmatic org/user provisioning
- Self-hosted with a single Docker container (plus a CockroachDB dependency)

### What Worked

The core multi-tenancy model mapped perfectly. ZITADEL Organization ID became Marten TenantId. Headless registration worked: the Angular SPA collects org data, calls the Organization Service, which calls ZITADEL's gRPC API to atomically create an Organization + Admin User.

### What Didn't

- **Custom login UI**: ZITADEL required a separate Next.js container (`zitadel-login`) for the login page. This was an extra container to manage, with its own build pipeline, for what should be a standard login form.
- **Service account credentials**: ZITADEL uses signed JWT files for service accounts, not standard OAuth2 client credentials. Each service needed a JWT file mounted into the container, managed separately from other config.
- **Org-scoped tokens**: We explored using ZITADEL's org-scoped access tokens (where the token itself is scoped to a specific org). This would have been elegant -- the token carries its tenant context natively. But org-scoped tokens in ZITADEL had limitations with the user management APIs and didn't compose well with the cross-tenant scenarios we needed for contractors.
- **.NET ecosystem**: The `Zitadel` NuGet package worked but was thin -- manual gRPC client setup, manual claim parsing, manual introspection configuration.

### The Switch to Keycloak

Keycloak 26 shipped with a native Organizations feature that maps 1:1 to our model. The `organization` claim is automatically included in tokens when the client has the `organization` scope. No custom Protocol Mapper required.

The migration removed:
- The `zitadel-login` container (Keycloak's hosted UI is sufficient; registration stays custom)
- All signed JWT files (standard `client_id` + `client_secret`)
- The `Zitadel` NuGet package (replaced by `Keycloak.AuthServices.*` with first-class .NET support)
- gRPC Organization API calls (replaced by REST Admin API)

A bonus: Keycloak uses UUIDs for all identifiers. This let us change external ID types from `string` to `Guid`, which is more natural in .NET and eliminates `Guid.Parse()` ambiguity.

The core architecture (direct tenant ID mapping, headless registration, BFF header injection) survived the IdP swap unchanged. The abstraction boundary was in the right place.

---

## Authorization: Two Layers, One Deferred

**The question**: How do you authorize access to individual resources in a multi-tenant system?

Layer 1 was free: Marten's conjoined multi-tenancy means every query is automatically scoped to the tenant. A landlord can never see another landlord's data. This covers the vast majority of access control.

Layer 2 was designed but not implemented: OpenFGA (Google Zanzibar-based Relationship-Based Access Control) for fine-grained permissions. The design:
- A hierarchical relationship model: Organization → Company → Property → Building → Unit
- `ListObjects` returns the set of resource IDs a user can access; the service filters query results
- Contextual tuples (ephemeral, request-scoped) for cross-tenant contractor access -- the Work Order database is the source of truth for assignments, and the tuple is passed with each check rather than persisted in the FGA store

This was deferred because the features that need it (Work Orders, contractor dashboards) haven't been built yet. The two-layer model remains the architectural intent.

---

## Frontend: From Material to Spartan

**The question**: What component library works with Tailwind CSS and gives you full control?

### The Angular Material Detour

The initial plan was pure Angular: Aria for headless accessible primitives, Material for complex widgets (datepicker, slider), CDK for structural utilities. A strict "no wrapper components" policy.

In practice, Material's usage crept beyond the intended datepicker/slider. Teams (even a team of one) naturally reach for the nearest pre-built component. Material's theming system (SCSS-based design tokens) fought with Tailwind at every turn. And despite the "no wrappers" policy, wrapper components appeared anyway.

### Spartan UI

Spartan is the Angular equivalent of shadcn/ui. It has two layers:
- **Brain** (npm packages): Headless accessible primitives. These are installed from npm and not modified.
- **Helm** (copied templates): Tailwind-styled component files that are generated into the project and fully owned by the team. You can modify them freely.

This model fits perfectly: Tailwind-native, no theming conflicts, full ownership of the visual layer, accessible primitives handled by the library. The Helm components live in `src/shared/components/ui/` and cover accordion, dialog, select, table, tabs, tooltip, and dozens more.

---

## Query-Time Joins vs Denormalized Projections

**The question**: When a Property list view needs to show the Company name, how do you resolve it?

The event sourcing instinct is to maintain a denormalized async projection: when a company name changes, update every Property projection that references it. But consider the math: one company might own thousands of properties. A single company rename triggers thousands of projection updates. That's O(n) write amplification.

The alternative: query-time joins. The list view handler issues two queries -- one for Properties, one for the referenced Companies -- and joins them in memory. This costs 2-3 queries per page request but has zero write amplification.

For a product where reads vastly outnumber writes but company renames are also realistic, query-time joins won. Marten's inline snapshot aggregates make this fast (the "join" is against a document, not a raw event replay).

---

## Cross-Service Data: Fat Events + Snapshot Seeding

**The question**: When the Property Service needs the Company name for display, and the Company Service owns that data, how does it get there?

Two channels:
1. **Integration events** (RabbitMQ): Fat events carrying all relevant fields. When a Company is created/updated/deleted, the event includes `CompanyId`, `OrganizationId`, `Code`, `Name`, `Timestamp`. The Property Service consumes these and maintains a local read model.
2. **HTTP snapshot endpoint**: For initial seeding (new service deployment) and disaster recovery. The Company Service exposes an internal endpoint that returns all companies for a tenant. The Property Service calls it once to bootstrap.

All writes use timestamp guards: if the incoming event/snapshot has an older timestamp than the local record, it's discarded. This makes processing order-independent and idempotent.

---

## Vertical Slices: Why Feature Folders

**The question**: How do you organize code in a microservice?

The traditional layered architecture (`Controllers/`, `Services/`, `Repositories/`) scatters a single feature across multiple folders. Adding a "Create Company" endpoint touches files in three or four directories.

Vertical slices group everything by feature:

```
Features/Companies/
  CompanyAggregate.cs
  CompanyEvents.cs
  CompanyEndpoints.cs
  Configuration/
  Lifecycle/
    CreateCompanyHandler.cs
    DeleteCompanyHandler.cs
    ...
```

Wolverine reinforces this: each handler is a self-contained class that receives a command and interacts with the session. There's no repository layer, no service layer, no mapper layer. The handler _is_ the feature implementation. This isn't just organizational -- it reduces the number of abstractions a developer needs to understand.

---

## The Infrastructure: Building a Free Cluster

**The question**: Can you run a production-like Kubernetes environment on commodity hardware for zero cost?

### Talos Linux

The cluster runs Talos Linux -- an immutable, API-managed Kubernetes OS. No SSH access, no package manager, no shell. All management is via `talosctl`. This is intentionally hostile to manual tinkering, which forces proper GitOps practices.

Three KVM virtual machines (1 control plane + 2 workers) run on an Ubuntu VM that itself runs on Hyper-V. Yes, it's nested virtualization. It works because Talos is minimal (no OS overhead) and the workloads are development-sized.

### Cilium as the Everything-Networking-Layer

Cilium replaces kube-proxy entirely and serves as: CNI (pod networking), L2 load balancer (ARP announcements for LoadBalancer services), and Gateway API implementation (HTTP routing, TLS termination). One component, four jobs.

### GitOps with ArgoCD

The entire cluster state is in Git. ArgoCD syncs from `deploy/environments/local/` via an App-of-Apps pattern. The root application points to sub-applications for platform services (Keycloak, cert-manager), workloads (RabbitMQ, Redis, PostgreSQL), and observability (Grafana, VictoriaMetrics, Tempo).

Infrastructure changes are commits. Rollbacks are reverts.

### Observability: The Free Stack

VictoriaMetrics (Prometheus-compatible metrics), VictoriaLogs (log aggregation), Tempo (distributed tracing), Grafana (dashboards), and Grafana Alloy (OpenTelemetry collector). All self-hosted, all open source. The same data that Aspire Dashboard gives you locally is available in the cluster via Grafana.

---

## AI-Assisted Development

**The question**: How do you make an AI coding assistant actually useful on a domain-specific project?

The answer: structured context. The `.github/` directory contains:

- **copilot-instructions.md**: Global project context -- tech stack, project structure, how to run, where to find docs. Applied to every conversation.
- **Context-scoped instructions**: `dotnet.instructions.md` is applied to all `*.cs` files. `angular.instructions.md` is applied to `landlord-portal/**`. The AI gets different instructions depending on what file you're editing.
- **Skills**: Step-by-step procedures for common tasks. `new-backend-feature` walks through creating an aggregate, events, handlers, endpoints, and configuration. The AI follows the procedure rather than guessing.
- **Prompts**: Reusable prompt templates for code review and documentation updates.

The `/docs/dev/` folder (backend feature structure, Angular feature structure, multi-tenancy flow) serves as the reference documentation that both humans and AI use. When the AI scaffolds a new feature, it follows the same patterns documented there.

---

## What We'd Do Differently

- **Start with Keycloak**: ZITADEL was a valuable learning experience, but if starting fresh, Keycloak's ecosystem maturity and .NET tooling make it the clear choice.
- **Start with Spartan UI**: The Angular Material detour consumed time that could have been spent on features.
- **Implement OpenFGA earlier**: Even a minimal setup would have validated the cross-tenant authorization model sooner.
- **CI/CD from week one**: The deployment pipeline is the simplest remaining piece, but having it early would have caught integration issues sooner.
