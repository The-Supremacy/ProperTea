# 2. Architecture Overview

This document outlines the high-level architectural decisions for the ProperTea platform. These decisions form the
technical foundation upon which the system is built.

---

## 2.1. Core Technology Stack

### Backend

- **Framework:** .NET 10 (using Minimal APIs)
- **Database:** PostgreSQL (following a database-per-service pattern)
- **Messaging:** Asynchronous communication via a message broker (RabbitMQ locally, Azure Service Bus in production).
- **Messaging Library:** Wolverine for implementing CQRS, outbox pattern, and sagas.
- **Identity Provider:** Self-hosted Keycloak for OIDC/OAuth2.
- **API Gateway:** A Backend-for-Frontend (BFF) gateway per portal, built with YARP.
- **Caching:** Redis for distributed caching.

### Frontend

- **Framework:** Next.js (App Router, TypeScript).
- **Architecture:** Separate Next.js applications for each user-facing portal (Landlord Office, Tenant, Market).

### Infrastructure & Deployment

- **Containerization:** Docker.
- **Orchestration:** Docker Compose (local dev) -> Kind (local K8s) -> Azure Kubernetes Service (AKS).
- **Infrastructure as Code (IaC):** Terraform.
- **Deployment:** GitOps using ArgoCD.

## 2.2. Architectural Patterns

### Monorepo

- **Decision:** The project will use a single monorepo.
- **Rationale:** Simplifies dependency management, enables atomic commits across services, and streamlines the CI/CD
  pipeline, which is highly beneficial in the early stages of a project.

### Microservices & Database-per-Service

- **Decision:** The domain will be broken down into distinct microservices, each owning its own database.
- **Rationale:** Enforces strong domain boundaries, allows services to be scaled and deployed independently, and enables
  technology diversity if needed in the future.

### Multi-Tenancy

- **Decision:** A "Shared Database, Shared Schema" approach will be used.
- **Rationale:** Data will be isolated at the row level using an `OrganizationId` column on all tenant-scoped entities.
  This is enforced automatically via EF Core Global Query Filters, providing a good balance of data isolation and
  operational simplicity.

### Inter-Service Communication

- **Synchronous:** For queries and direct commands where an immediate response is needed, services will communicate via
  RESTful APIs (HTTP).
- **Asynchronous:** For domain events and decoupling services, communication will happen via a message broker. This
  ensures eventual consistency and improves system resilience.

## 2.3. Authentication & Authorization

### Authentication: Backend-for-Frontend (BFF)

- **Decision:** A BFF gateway will manage authentication for each frontend client.
- **Flow:** The frontend uses a secure, `HttpOnly` session cookie to talk to the BFF. The BFF holds the JWTs from
  Keycloak and attaches them to downstream requests to backend services.
- **Rationale:** This pattern provides significant security benefits by preventing JWTs from ever being exposed to the
  browser, thus mitigating XSS-based token theft.

### Authorization: Hybrid RBAC + ReBAC

- **Decision:** A hybrid model combining Role-Based Access Control (RBAC) and Relationship-Based Access Control (ReBAC)
  will be used.
- **RBAC (Coarse-Grained):** Broad permissions are defined by roles included in the user's JWT (e.g.,
  `property_manager`). This allows for fast, stateless checks at the API gateway or service entry point.
- **ReBAC (Fine-Grained):** For specific access checks (e.g., "Can this user edit *this specific* property?"), a
  dedicated **Permission Service** is queried. This service understands the relationships between users and resources.
- **Rationale:** This hybrid approach provides defense-in-depth, combining the performance of RBAC with the precision of
  ReBAC.

## 2.4. API Strategy

- **Decision:** API versioning will be done via the URL path (e.g., `/api/v1/{resource}`).
- **Rationale:** This is an explicit, widely understood, and easy-to-manage standard for REST API versioning.

## 2.5. Testing Strategy

- **Decision:** A standard test pyramid will be implemented.
- **Levels:**
    - **Unit Tests (xUnit):** Form the base, testing individual components in isolation.
    - **Integration Tests (xUnit + Testcontainers):** Verify interactions with external dependencies like databases and
      message brokers.
    - **Contract Tests (Pact):** Ensure that service-to-service API contracts are not broken.
    - **End-to-End Tests (Playwright):** Validate full user flows through the system.

## 2.6. Observability

- **Decision:** The system will be instrumented using the **OpenTelemetry** standard.
- **Local Stack:** A full local observability stack (Prometheus, Loki, Grafana, Tempo) will be used for development and
  testing.
- **Production:** In Azure, all telemetry data will be sent to **Azure Monitor** for unified logging, tracing, and
  metrics.

## 2.7. Repository Structure

