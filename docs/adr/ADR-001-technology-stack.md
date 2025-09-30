# ADR-001: Core Technology Stack Selection
# ADR-001: Core Technology Stack Selection

## Status
Accepted

## Context

ProperTea is a learning-focused property management platform that needs to demonstrate modern cloud-native patterns while remaining cost-effective and maintainable. The system requires:

- Multi-tenant SaaS architecture
- Local development without external dependencies
- Production deployment on Azure
- Modern observability and security practices
- Educational value for learning cloud-native development

Key constraints:
- Monorepo approach for simplified cross-service changes
- No commercial/paid libraries where free alternatives exist
- Production parity in local development
- Future migration path from Azure Container Apps to AKS

## Decision

### Backend Services
- **.NET 9**: Latest LTS framework with native container support
- **PostgreSQL**: Primary database (local and Azure Flexible Server)
- **Entity Framework Core 9**: ORM with code-first migrations per service
- **YARP (Yet Another Reverse Proxy)**: API Gateway and reverse proxy
- **ASP.NET Core Identity**: Authentication foundation
- **Microsoft.FeatureManagement**: Feature flags with custom org-scoped provider

### Frontend
- **Next.js 15**: React framework with App Router and TypeScript
- **TailwindCSS**: Utility-first CSS framework
- **Material-UI (MUI)**: Component library for maximum ready components
- **React Query**: Server state management
- **Zustand**: Minimal client state management
- **next-auth**: Authentication integration

### Infrastructure & Operations
- **Docker Compose**: Local development orchestration
- **Traefik**: Local reverse proxy with TLS termination
- **mkcert**: Local TLS certificate generation
- **GitHub Actions**: CI/CD with reusable workflows
- **Bicep**: Infrastructure as Code for Azure resources
- **Helm**: Kubernetes manifests for future AKS migration

### Messaging & Events
- **Kafka**: Event streaming (local development)
- **Azure Service Bus**: Production messaging with interface abstraction
- **CloudEvents**: Standardized event format
- **Custom outbox pattern**: Transactional event publishing

### Observability
- **OpenTelemetry**: Unified observability SDK across all environments
- **Jaeger**: Distributed tracing with persistent storage
- **Prometheus**: Metrics collection with long-term retention
- **Grafana**: Dashboards, visualization, and alerting
- **Loki**: Log aggregation with Azure Blob backend in production
- **Optional Azure Monitor**: Secondary export for comparison and compliance

### Storage & Caching
- **Redis**: Caching, distributed locks, session storage
- **OpenSearch**: Full-text search and property indexing (local)
- **Azure Cognitive Search**: Production search service
- **Azurite**: Local blob storage emulation
- **Azure Blob Storage**: Production object storage

### Testing
- **xUnit**: Unit testing framework
- **Testcontainers**: Integration testing with real dependencies
- **Playwright**: End-to-end testing for frontend and APIs
- **PactNet**: Consumer-driven contract testing

## Consequences

### Positive
- **Modern Stack**: Latest .NET 9, Next.js 15, and cloud-native patterns
- **Complete Production Parity**: Identical development and production environments
- **Production Ready**: Direct path to Azure Container Apps and future AKS
- **Educational Value**: Deep expertise with industry-standard observability tools
- **Cost Predictable**: Open-source observability stack with known infrastructure costs
- **Maintainable**: Well-documented, widely-adopted technologies with strong community
- **Scalable**: Microservices architecture with proper separation of concerns
- **Vendor Independence**: Full control over observability and monitoring stack

### Negative
- **Complexity**: Multiple technologies and services to learn and maintain
- **Learning Curve**: Significant initial setup and learning investment
- **Resource Usage**: Docker Compose stack requires substantial local resources
- **Version Management**: Keeping multiple technology stacks updated
- **Debugging Complexity**: Distributed system debugging challenges

### Risk Mitigations
- **Documentation**: Comprehensive setup guides and troubleshooting docs
- **Makefile**: Simplified common operations and bootstrapping
- **Health Checks**: Comprehensive monitoring of all services
- **Fallback Options**: Interface abstractions allow technology swapping
- **Migration Path**: Helm charts prepared for AKS transition
- **Observability Expertise**: Team develops deep knowledge of production monitoring tools
- **Cost Control**: Predictable infrastructure costs vs. usage-based Azure Monitor pricing
## Status
Accepted

## Context
ProperTea is a property management platform that needs modern cloud-native patterns, local development without external dependencies, and a production path on Azure.

Key constraints:
- Monorepo; no paid libraries where free options exist
- Production parity locally
- Migration path from Azure Container Apps to AKS

## Decision

### Backend
- .NET 9, EF Core 9, PostgreSQL (Azure Flexible Server in prod)
- YARP as API Gateway
- ASP.NET Core Identity for auth
- Microsoft.FeatureManagement for feature flags (custom org/group provider)

### Frontend
- Next.js 15, TailwindCSS, MUI
- React Query + minimal Zustand
- next-auth for auth integration

### Infra & Ops
- Docker Compose locally
- Traefik + mkcert for HTTPS
- GitHub Actions CI/CD
- Bicep for Azure IaC; Helm-friendly manifests for future AKS

### Messaging & Events
- Kafka locally; Azure Service Bus later via interface
- CloudEvents JSON
- Outbox pattern per service

### Observability
- OpenTelemetry SDK
- Local: Jaeger, Prometheus, Grafana, Loki
- Prod: Azure Monitor/App Insights via OTel exporters

### Storage & Caching
- Redis for cache/locks/data protection keys
- OpenSearch locally; Azure Cognitive Search later
- Azurite locally; Azure Blob in prod

### Testing
- xUnit, Testcontainers, Playwright, PactNet

## Consequences

Positive:
- Modern, widely adopted components
- Full local stack; strong learning value
- Clear Azure production path

Negative:
- Multi-tool complexity and resource usage
- Distributed debugging complexity

Mitigations:
- Thorough docs and Makefile targets
- Health checks and observability defaults
- Interface abstractions for portability
