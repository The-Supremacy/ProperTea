# ProperTea

**A Multi-Tenant Real Estate ERP for Modern Property Management**

ProperTea is a cloud-native, event-driven property management platform built on .NET with a microservices architecture. It provides comprehensive tools for landlords to manage properties, tenants, rentals, maintenance, and financials across multiple legal entities.

## 🏗️ Architecture

- **Backend**: .NET 9.0 microservices with Event Sourcing (Marten + PostgreSQL)
- **Messaging**: Wolverine (CQRS) over RabbitMQ
- **Multi-Tenancy**: Marten conjoined tenancy with organization-level isolation
- **Authentication**: ZITADEL (External IdP) with JWT bearer tokens
- **Authorization**: OpenFGA for fine-grained permissions (planned)
- **Frontend**: Angular 21+ with Tailwind CSS (Headless: Angular Aria + Spartan UI)
- **Orchestration**: .NET Aspire for local development

## 📁 Repository Structure

```
ProperTea/
├── apps/
│   ├── services/               # Backend microservices
│   │   ├── ProperTea.Organization/  # Tenant master & registration
│   │   ├── ProperTea.User/          # User profiles & preferences
│   │   ├── ProperTea.Company/       # Legal business entities
│   │   ├── ProperTea.Property/      # Physical assets (planned)
│   │   └── ProperTea.Rental/        # Commercial operations (planned)
│   └── portals/
│       └── landlord/
│           ├── bff/            # Backend for Frontend (YARP + Typed Clients)
│           └── web/            # Angular SPA
├── shared/
│   ├── ProperTea.Contracts/         # Integration event contracts
│   ├── ProperTea.Infrastructure.Common/  # Shared utilities
│   └── ProperTea.ServiceDefaults/   # Common service configuration
├── orchestration/
│   └── ProperTea.AppHost/      # .NET Aspire orchestrator
└── docs/
    ├── architecture.md         # System architecture overview
    ├── domain.md              # Domain model & business rules
    ├── event-catalog.md       # Integration events catalog
    └── decisions/             # Architecture Decision Records (ADRs)
```

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for infrastructure)
- Node.js 20+ (for frontend)

### Running Locally

```bash
# Start all services with Aspire
dotnet run --project orchestration/ProperTea.AppHost

# Access services
# - Aspire Dashboard: https://localhost:17285
# - Landlord Portal: http://localhost:4200
# - ZITADEL: http://localhost:8080
# - RabbitMQ: http://localhost:15672
```

### Development Workflow

1. **Aspire Dashboard** shows all running services, logs, and traces
2. **Service-specific docs** in each service's README.md
3. **System-wide docs** in `/docs/`
4. **ADRs** document architectural decisions in `/docs/decisions/`

## 🏛️ Services

### Organization Service
The "Tenant Master" that orchestrates headless registration with ZITADEL and publishes lifecycle events.
- 📄 [Service README](apps/services/ProperTea.Organization/README.md)

### User Service
Manages user profiles, preferences, and activity tracking within organizations.
- 📄 [Service README](apps/services/ProperTea.User/README.md)

### Company Service
Manages legal business entities (LLCs, Corporations) that own properties and conduct operations.
- 📄 [Service README](apps/services/ProperTea.Company/README.md)

### Landlord BFF
Backend for Frontend providing authentication, session management, and service aggregation.
- 📄 [Service README](apps/portals/landlord/bff/README.md)

## 📚 Documentation

- **[Architecture](docs/architecture.md)**: System design, patterns, and service boundaries
- **[Domain Model](docs/domain.md)**: Business rules and aggregates
- **[ADRs](docs/decisions/)**: Architecture Decision Records
- **[Dev Guides](docs/dev/)**: Development patterns and quirky behavior

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific service tests
dotnet test apps/services/ProperTea.Company
```

## 🔧 Technology Stack

### Backend
- .NET 10
- Marten (Event Store + Document DB)
- Wolverine (CQRS + Messaging)
- PostgreSQL
- RabbitMQ
- Redis

### Frontend
- Angular 21+ (Standalone Components, Signals)
- Angular Aria (Headless accessible components)
- Spartan UI (shadcn-style components)
- TanStack Table (Data grids)
- Tailwind CSS (Styling)
- Transloco (i18n)

### Infrastructure
- .NET Aspire (Orchestration)
- ZITADEL (Authentication)
- OpenFGA (Authorization - planned)
- MailPit (Email testing)

## 📖 Key Patterns

- **Vertical Slice Architecture**: Features organized by capability, not layer
- **Event Sourcing**: Domain events as source of truth for aggregates
- **CQRS**: Commands and queries handled by separate Wolverine handlers
- **BFF Pattern**: Frontend-specific API gateway with no business logic
- **Multi-Tenancy**: Organization-scoped data isolation via Marten

## 🤝 Contributing

1. Read the architecture docs in `/docs/`
2. Check ADRs for context on past decisions
3. Follow patterns established in existing services
4. Service-specific guidance in each service's README.md

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
