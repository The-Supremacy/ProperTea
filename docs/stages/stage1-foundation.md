# Implementation Plan: Stage 1 - Foundation

### Goal
Validate all cross-cutting concerns (auth, tenancy, events, CQRS) with minimal domain complexity, running entirely within Docker Compose.

---

## 1.1. Core Services to Build

1.  **BFF Gateway (YARP):** Manages user sessions (via secure cookies), handles the OIDC flow with Keycloak, and acts as a reverse proxy to backend services, attaching JWTs to requests.
2.  **Identity Service:** Manages user profiles and the mapping between users and organizations. Publishes events like `UserRegistered`.
3.  **Organization Service:** Manages the lifecycle of an Organization (the system tenant). The source of truth for `OrganizationId`.
4.  **Permission Service:** Implements the fine-grained authorization checks (ReBAC). Provides an API for other services to check permissions.
5.  **Property Management Service:** The first core domain service. Manages properties and units, enforcing multi-tenancy.

## 1.2. Infrastructure (Local)

- **Orchestration:** Docker Compose
- **Services:**
    - Keycloak
    - PostgreSQL (one instance per service)
    - RabbitMQ
    - Redis

## 1.3. Step-by-Step Action Plan

### Phase 1: Project & Infrastructure Setup

1.  **Initialize Monorepo:** Create the repository with the directory structure outlined in the Architecture Overview.
2.  **Setup Docker Compose:**
    - Create a `docker-compose.yml` file in `infrastructure/docker-compose/`.
    - Add service definitions for Keycloak, PostgreSQL, RabbitMQ, and Redis.
    - Configure networking and volumes.
3.  **Create Shared Libraries:**
    - In `src/shared/`, create initial projects for:
        - `ProperTea.Common`: Common abstractions, helpers.
        - `ProperTea.Auth`: JWT validation middleware and claims handling.
        - `ProperTea.MultiTenancy`: Tenant context provider and middleware.
        - `ProperTea.Messaging`: Base configuration for Wolverine and RabbitMQ.
4.  **Scaffold Core Services:**
    - In `src/services/`, create the five initial .NET service projects.
    - Add a basic `Dockerfile` to each service.
    - Add service definitions to the `docker-compose.yml` file, ensuring they depend on the infrastructure.
    - Implement a basic `/health` endpoint in each service.
5.  **Setup CI/CD:** Create a basic GitHub Actions workflow that builds the .NET projects and runs `docker-compose build`.

### Phase 2: Identity, Auth & Tenancy

1.  **Configure Keycloak:**
    - Set up a realm, clients (for the BFF), and initial roles.
    - Document the setup process.
2.  **Implement BFF:**
    - Configure YARP to route traffic.
    - Implement the OIDC flow to authenticate users against Keycloak.
    - Implement session management using Redis.
3.  **Implement Identity & Organization Services:**
    - Build the CRUD APIs for users and organizations.
    - Implement the logic to sync user data from Keycloak.
4.  **Implement Multi-Tenancy Middleware:**
    - In the shared library, create the `ICurrentOrganizationProvider` and the ASP.NET Core middleware to populate it from the JWT claim.
    - All services should include this middleware.

### Phase 3: Core Domain & Permissions

1.  **Implement Permission Service:**
    - Design the database schema for roles and relationships.
    - Create the `POST /api/v1/permissions/check` endpoint.
    - Implement Redis caching for check results.
2.  **Implement Property Management Service:**
    - Define the `Property` and `Unit` entities, ensuring they include `OrganizationId`.
    - Implement CQRS handlers using Wolverine for creating and updating properties.
    - Configure the DbContext to use the EF Core Global Query Filter, injecting the `ICurrentOrganizationProvider`.
3.  **Implement Eventing:**
    - Configure the Wolverine outbox pattern in the Property service.
    - Publish a `PropertyCreated` event when a new property is added.
    - Create a simple consumer in another service (e.g., Vacancy Service placeholder) to verify the event is received via RabbitMQ.

### Phase 4: Validation & Testing

1.  **Write Integration Tests:** For each service, create tests that verify database interactions and multi-tenancy logic. Use Testcontainers to spin up dependencies.
2.  **Manual E2E Test:** Perform a full flow:
    - Create an organization.
    - Create a user and assign them to the organization.
    - Log in as the user.
    - Create a property for that organization.
    - Attempt to access the property with a user from a different organization and verify access is denied.
3.  **Document APIs:** Generate OpenAPI/Swagger documentation for all services.
