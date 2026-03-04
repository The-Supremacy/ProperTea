# ProperTea

A multi-tenant Real Estate ERP built as a learning project to explore the full cloud-native .NET stack -- from event sourcing to Kubernetes GitOps -- without paying for any external service.

## Architecture

- **Backend**: .NET 10 microservices with Event Sourcing (Marten + PostgreSQL)
- **Messaging**: Wolverine (CQRS) over RabbitMQ
- **Multi-Tenancy**: Marten conjoined tenancy with organization-level isolation
- **Authentication**: Keycloak 26+ with Organizations feature
- **Authorization**: Marten multi-tenancy (automatic). OpenFGA planned for fine-grained permissions.
- **Frontend**: Angular 21 with Tailwind CSS (Spartan UI + Angular Aria)
- **Orchestration**: .NET Aspire for local development
- **Kubernetes**: Talos Linux cluster on KVM, Cilium networking, ArgoCD GitOps

## Repository Structure

```
ProperTea/
  apps/
    services/                    # Backend microservices
      ProperTea.Organization/    # Tenant master and registration
      ProperTea.User/            # User profiles and preferences
      ProperTea.Company/         # Legal business entities
      ProperTea.Property/        # Physical assets (properties, buildings, units)
    portals/
      landlord/
        bff/                     # Backend for Frontend (YARP + sessions)
        web/                     # Angular SPA
  shared/
    ProperTea.Contracts/              # Integration event contracts
    ProperTea.Infrastructure.Common/  # Shared utilities
    ProperTea.ServiceDefaults/        # Common service configuration
  orchestration/
    ProperTea.AppHost/           # .NET Aspire orchestrator
  deploy/
    environments/local/          # Talos cluster config, ArgoCD apps, Kustomize overlays
    infrastructure/base/         # Shared Kustomize bases (platform, workloads, o11y)
  docs/
    tech-overview.md             # Technology showcase (start here)
    project-journal.md           # Design decisions and pivots narrative
    architecture.md              # System architecture reference
    domain.md                    # Domain model and business rules
    dev/                         # Development patterns (AI-assisted dev reference)
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for infrastructure containers)
- Node.js 22+ (for frontend)

### Running Locally

```bash
# Start all services with Aspire
dotnet run --project orchestration/ProperTea.AppHost

# Access services
# Aspire Dashboard: https://localhost:17285
# Landlord Portal:  http://localhost:4200
# Keycloak:         http://localhost:9080
# RabbitMQ:         http://localhost:56720
```

## Services

### Organization Service
The "Tenant Master" that orchestrates headless registration with Keycloak and publishes lifecycle events.

### User Service
Manages user profiles, preferences, and activity tracking within organizations.

### Company Service
Manages legal business entities (companies) that own properties and conduct operations.

### Property Service
Manages the physical reality: properties, buildings, entrances, and units.

### Landlord BFF
Backend for Frontend providing authentication, session management, and reverse proxying to backend services.

## Documentation

| Document | Purpose |
|---|---|
| [Technology Showcase](docs/tech-overview.md) | Presentation-friendly overview of all technologies and patterns |
| [Project Journal](docs/project-journal.md) | Narrative of design decisions and pivots |
| [Architecture](docs/architecture.md) | System design, patterns, and service boundaries |
| [Domain Model](docs/domain.md) | Business rules, aggregates, and ubiquitous language |
| [Dev Guides](docs/dev/) | Development patterns (backend features, Angular features, multi-tenancy) |

## Technology Stack

### Backend
- .NET 10
- Marten (Event Store + Document DB)
- Wolverine (CQRS + Messaging)
- PostgreSQL, RabbitMQ, Redis

### Frontend
- Angular 21 (Standalone Components, Signals, Zoneless)
- Spartan UI (Brain + Helm, shadcn-style)
- TanStack Table, Angular Aria
- Tailwind CSS 4, Transloco (i18n)

### Infrastructure
- .NET Aspire (Local orchestration)
- Keycloak 26+ (Authentication with Organizations)
- Talos Linux on KVM (Kubernetes cluster)
- Cilium (CNI + Gateway API + L2 LB)
- ArgoCD + Kustomize (GitOps)
- VictoriaMetrics + Tempo + Grafana (Observability)
- Longhorn (Distributed storage)
- SOPS + age (Secret management)

## Key Patterns

- **Vertical Slice Architecture**: Features organized by capability, not layer
- **Event Sourcing**: Domain events as source of truth for aggregates (Decider pattern)
- **CQRS**: Commands and queries handled by separate Wolverine handlers
- **BFF Pattern**: Frontend-specific API gateway with no business logic
- **Multi-Tenancy**: Keycloak org ID used directly as Marten TenantId
- **AI-Assisted Development**: Structured Copilot instructions, skills, and prompts

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
