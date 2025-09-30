# ProperTea System Architecture Overview
# ProperTea System Architecture Overview

## Purpose and Scope

ProperTea is a multi-tenant property management platform built as a microservices architecture to support property owners, landlords, and tenants across multiple organizations. The system demonstrates modern cloud-native patterns, observability, and security practices.

## System Context

### Main Actors
- **Property Owners/Landlords**: Manage properties, tenants, contracts through Landlord Portal
- **Support Team**: Cross-organization technical support with limited permissions
- **System Administrators**: Infrastructure-level access for emergency scenarios

### Key Business Domains
- **Identity & Access Management**: User authentication, authorization, and permissions
- **Organization Management**: Multi-tenant organization and company structure
- **Property Management**: Property listings, details, and lifecycle management
- **Search & Discovery**: Property search with filters, faceting, and autocomplete

## Service Architecture

### Core Services
1. **Identity Service**: Authentication, credentials, MFA, external providers (Entra ID)
2. **Authorization Service**: Permissions model, access control decisions
3. **User Management Service**: User profiles, organization memberships, user groups
4. **Organizations Service**: Organization settings, feature flags
5. **Companies Service**: Company profiles within organizations
6. **Property Service**: Property management and listings
7. **Search Indexer Service**: Property search indexing and queries
8. **Workflow Orchestration Service**: Saga coordination, multi-service workflows
9. **API Gateway (YARP)**: Single public entry point, routing, auth, rate limiting
10. **Landlord Portal (Next.js)**: Frontend application with BFF capabilities

### Infrastructure Services
- **Kafka**: Event streaming and async messaging
- **PostgreSQL**: Primary data store with shared-table multitenancy
- **Redis**: Caching, distributed locks, session storage
- **OpenTelemetry Stack**: Jaeger, Prometheus, Grafana, Loki for observability
- **OpenSearch**: Full-text search and property indexing
- **Azurite**: Local blob storage emulation

## Data Flow Architecture
## Purpose and Scope
ProperTea is a multi-tenant property management platform built as a microservices architecture to support property owners, landlords, and tenants across multiple organizations. The system demonstrates modern cloud-native patterns, observability, and security practices with production parity locally.

## System Context
- Property Owners/Landlords: Manage properties, tenants, contracts via Landlord Portal
- Support Team: Cross-organization support with limited permissions
- System Administrators: Infrastructure-level emergency access, audited

## Core Services
1. Identity Service: Authentication, credentials, MFA, external providers (Entra ID)
2. Authorization Service: Permissions model, effective permissions per user+org
3. User Management Service: Profiles, org memberships, user groups, invitations, preferences sync
4. Organizations Service: Org settings, feature flags (org-level)
5. Companies Service: Company profiles (within orgs)
6. Property Service: Property management and listings
7. Search Indexer Service: Indexing to OpenSearch and simple search
8. Workflow Orchestration Service: Sagas and long-running workflows
9. API Gateway (YARP): Single public entry, auth/routing/rate limits/observability
10. Landlord Portal (Next.js): Frontend, BFF for UI concerns, calls Gateway

## Infrastructure (local)
- Kafka, PostgreSQL, Redis
- OpenTelemetry stack: Collector, Jaeger, Prometheus, Grafana, Loki
- OpenSearch (local search)
- Azurite (blob storage emulation)
- Traefik + mkcert for HTTPS parity

## Data Flow
Frontend (Next.js) → API Gateway (YARP) → Backend Services
                                  ↘
                         Authorization Service (permissions)
                                  ↘
                          Postgres + Redis + Kafka

### Request Flow (high level)
1) FE sends Identity JWT to Gateway (10 min access, 30-day refresh).
2) Gateway validates JWT, checks org access, fetches permissions for user+org.
3) Gateway mints internal JWT (60s) embedding permissionsByService and forwards request with X-Org-Id.
4) Services validate internal token and perform local permission checks + business logic.
5) Responses return via Gateway; OTel traces/metrics/logs captured.

## Multitenancy
- URL: /api/v0/organization/{organizationId}/service/endpoint
- Shared-table with OrganizationId discriminator (EF global filters)
- Gateway rejects requests if org not in user.orgs[]
- Future migration path: schema-per-tenant or database-per-tenant

## Communication
- REST over HTTPS for external and internal calls (typed clients)
- gRPC optional later (internal)
- Asynchronous events (CloudEvents JSON) via Kafka locally; ASB later

## Security
- External Identity JWT only between FE and Gateway
- Internal short-lived JWT minted by Gateway (iss=https://gateway.local, aud=internal-services)
- Rate limiting at Gateway keyed by (route, userId, orgId|none, IP)
- Support/emergency permissions time-limited and audited

## Observability
- OpenTelemetry everywhere
- Local: Jaeger (traces), Prometheus/Grafana (metrics), Loki (logs)
- Prod: Azure Monitor/App Insights via OTel exporters
- Health endpoints: /health/live, /health/ready; /metrics for scraping

## Environments
- Local: Full stack via Docker Compose, HTTPS with Traefik/mkcert, no external cloud deps
- Cloud: Azure Container Apps initially; AKS path via Helm-compatible manifests
- Secrets: .env locally; Key Vault + Managed Identity in prod

## Next Steps
- See Services specs in docs/services/
- See Security model in docs/security/security-model.md
- See API standards (incl. RFC 7807) in docs/apis/standards.md
