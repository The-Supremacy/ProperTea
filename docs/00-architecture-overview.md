# ProperTea - System Architecture Overview

**Version:** 1.0.1  
**Last Updated:** October 29, 2025  
**Status:** MVP 1 Architecture - Approved for Implementation

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Vision](#system-vision)
3. [Architectural Principles](#architectural-principles)
4. [Technology Stack](#technology-stack)
5. [Service Architecture](#service-architecture)
6. [Data Flow Patterns](#data-flow-patterns)
7. [Deployment Topology](#deployment-topology)
8. [Cross-Cutting Concerns](#cross-cutting-concerns)
9. [Design Decisions & Rationales](#design-decisions--rationales)

---

## Executive Summary

ProperTea is a cloud-native, event-driven ERP system for real estate and lease management. The architecture prioritizes
**educational value** while maintaining production readiness, enabling hands-on learning with modern microservices
patterns.

### Key Characteristics

- **15 Microservices** (11 domain services, 4 infrastructure services)
- **Event-Driven Architecture** with Kafka/Azure Service Bus
- **CQRS + DDD** patterns for business logic
- **Kubernetes-Native** (portable across k3s/Kind/AKS/ACA)
- **Multi-Tenant** with organization-scoped data isolation
- **BFF Pattern** for client-specific gateways

### Primary Use Cases

1. **Property Management** - Register and manage buildings, rental objects, components
2. **Market Operations** - Publish listings, manage applications and offers
3. **Lease Management** - Create, approve, sign, and terminate lease agreements
4. **Maintenance & Dwelling Inspection** - Handle fault notifications, work orders, dwelling inspections
5. **User & Permission Management** - Multi-org user access with fine-grained permissions

---

## System Vision

### Business Context

ProperTea serves three primary user groups:

1. **Property Managers** (Landlord Portal)
    - Manage properties across multiple organizations
    - Publish rental objects to market
    - Approve lease agreements
    - Assign maintenance work orders

2. **Tenants** (Tenant Portal)
    - View lease agreements and invoices
    - Report maintenance issues
    - Receive notifications

3. **Market Users** (Market Portal)
    - Search available rental objects
    - Submit applications
    - Negotiate offers

### Technical Vision

- **Portable Architecture**: Works on local Kind and Azure AKS
- **Educational Focus**: Custom implementations over black-box libraries (CQRS, Sagas, Integration Events)
- **Production Ready**: Observability, security, and scalability built-in
- **Developer Experience**: Multi-mode debugging (inner loop → full platform → k8s validation)

---

## Architectural Principles

### 1. **Cloud-Native & Portable**

**Principle:** Design for Kubernetes, avoid cloud-specific lock-in.

**Implementation:**

- Standard k8s patterns (Deployments, Services, Ingress)
- Abstracted dependencies (Kafka ↔ ServiceBus, SeaweedFS ↔ Azure Blob)
- Helm charts for all services
- Works on: Kind (local), Kind (CI), AKS (prod), AKS (future)

**Example:**

```csharp
// Abstraction allows swapping providers
builder.Services.AddProperIntegrationEvents()
    .UseKafka(config)       // Local, AKS
    // .UseServiceBus(config)  // ACA production
```

### 2. **Event-Driven Choreography + Orchestration**

**Principle:** Use events for loose coupling, sagas for critical workflows.

**When to Use:**

| Pattern                  | Use Case                                     | Example                                                                                                     |
|--------------------------|----------------------------------------------|-------------------------------------------------------------------------------------------------------------|
| **Choreography**         | Independent actions, eventual consistency OK | Organization created → Permission service seeds default groups                                              |
| **Orchestration (Saga)** | Multi-step workflow, compensation needed     | Contact GDPR cleanup → Validate with all services holding personal data → Compensate if any service rejects |

**Implementation:**

- Outbox pattern for reliable event publishing
- Custom saga library (`ProperTea.ProperSagas`) for orchestration
- Event-driven workers (separate projects per service)

### 3. **Domain-Driven Design**

**Principle:** Business logic in domain layer, separated from infrastructure.

**Structure:**

```
Service/
├── ProperTea.{Service}.Service/       # API (Controllers → Application layer)
├── ProperTea.{Service}.Worker/        # Event consumers
├── ProperTea.{Service}.Domain/        # Aggregates, entities, domain events
│   ├── Aggregates/
│   │   └── Property.cs                # Aggregate root
│   ├── Events/
│   │   └── PropertyCreatedEvent.cs
│   └── Services/
│       └── PropertyDomainService.cs
└── ProperTea.{Service}.Infrastructure/ # EF, repositories, external APIs
```

**Key Patterns:**

- Aggregate roots as transaction boundaries
- Domain events within aggregates
- Integration events published via outbox
- Repository per aggregate root

### 4. **CQRS for Read/Write Separation**

**Principle:** Commands change state, queries read state. Separate models when needed.

**Implementation:**

- Commands: `CreatePropertyCommand`, `PublishListingCommand`
- Queries: `GetPropertiesQuery`, `GetAvailableListingsQuery`
- Command/query buses via `ProperTea.ProperCqrs`
- Validation decorators for commands

**Example:**

```csharp
// Command (write)
var command = new CreatePropertyCommand(companyId, address, buildingData);
var propertyId = await _commandBus.SendAsync(command);

// Query (read)
var query = new GetPropertiesQuery(companyId, filters);
var properties = await _queryBus.SendAsync(query);
```

### 5. **BFF (Backend for Frontend) Pattern**

**Principle:** Each client type has dedicated gateway for session management and request routing.

**Why:**

- ✅ Isolates client-specific logic (session cookies for web, future mobile auth)
- ✅ Aggregates backend calls (reduces frontend complexity)
- ✅ Security boundary (JWT enrichment, internal services not exposed)

**BFFs:**

- **Landlord BFF** - Org-scoped, permission-enriched JWT
- **Tenant BFF** - User-scoped, personal data access
- **Market BFF** - Public + user-scoped (applications)

### 6. **Observability-First**

**Principle:** Every service emits structured logs, metrics, and traces from day 1.

**Implementation:**

- OpenTelemetry SDK in all services
- Dual-stack: Local (Jaeger, Loki, Prometheus) / Production (Azure Monitor)
- Distributed tracing spans critical flows (Contact GDPR deletion saga, listing publication)
- Metrics: Event processing latency, API response times, saga durations

---

## Technology Stack

### Backend Services

| Component                   | Technology                | Version  | Purpose                             |
|-----------------------------|---------------------------|----------|-------------------------------------|
| **Runtime**                 | .NET                      | 9.0      | All services                        |
| **API Framework**           | ASP.NET Core Minimal APIs | 9.0      | HTTP endpoints                      |
| **Database**                | PostgreSQL                | 17       | Per-service databases               |
| **ORM**                     | Entity Framework Core     | 9.0      | Data access                         |
| **Resilience**              | .NET 9 Resilience APIs    | Built-in | Retries, circuit breakers, timeouts |
| **Caching**                 | Redis                     | 7        | Session storage, permission cache   |
| **Messaging (Local)**       | Kafka                     | 4.0      | Event streaming                     |
| **Messaging (Prod)**        | Azure Service Bus         | Managed  | Event streaming                     |
| **Search**                  | Elasticsearch             | 9.0      | Full-text search, autocomplete      |
| **Blob Storage (Local)**    | Azurite                   | Latest   | Azure Blob emulator (primary)       |
| **Blob Storage (Optional)** | SeaweedFS                 | Latest   | Self-hosted S3 (educational)        |
| **Blob Storage (Prod)**     | Azure Blob                | Managed  | Document storage                    |

### Shared Libraries (NuGet Packages)

| Library                             | Purpose                                                |
|-------------------------------------|--------------------------------------------------------|
| `ProperTea.ProperCqrs`              | Command/query buses, validation decorators             |
| `ProperTea.ProperDdd`               | Aggregate roots, entities, domain events, repositories |
| `ProperTea.ProperIntegrationEvents` | Event bus abstraction, outbox pattern                  |
| `ProperTea.ProperSagas`             | Saga orchestration base classes                        |
| `ProperTea.ProperStorage`           | Blob storage abstraction (SeaweedFS/Azure Blob)        |
| `ProperTea.ProperTelemetry`         | OpenTelemetry configuration                            |
| `ProperTea.ProperErrorHandling`     | Global exception handling                              |

### Infrastructure (Local Development)

| Component           | Technology                        | Purpose                           |
|---------------------|-----------------------------------|-----------------------------------|
| **Orchestration**   | Kind                              | Local k8s cluster                 |
| **Package Manager** | Helm                              | Service deployments               |
| **Ingress**         | Traefik                           | HTTP routing, TLS termination     |
| **Observability**   | Jaeger, Loki, Prometheus, Grafana | Traces, logs, metrics, dashboards |

### Infrastructure (Production - AKS)

| Component              | Technology                           | Purpose                       |
|------------------------|--------------------------------------|-------------------------------|
| **Container Platform** | Azure Kubernetes Service (AKS)       | Managed Kubernetes            |
| **Ingress**            | NGINX Ingress Controller             | HTTP routing, TLS termination |
| **Service Mesh**       | None (deferred to post-MVP)          | Future: Istio or Linkerd      |
| **Database**           | Azure PostgreSQL Flexible            | Managed Postgres              |
| **Caching**            | Azure Cache for Redis                | Managed Redis                 |
| **Messaging**          | Azure Service Bus                    | Managed messaging             |
| **Storage**            | Azure Blob Storage                   | Managed blob storage          |
| **Secrets**            | Azure Key Vault                      | Secret management             |
| **Observability**      | Azure Monitor / Application Insights | Managed observability         |
| **IaC**                | Bicep                                | Infrastructure provisioning   |

### Frontend (Future)

| Component      | Technology                     | Purpose               |
|----------------|--------------------------------|-----------------------|
| **Framework**  | Next.js                        | SSR, routing          |
| **UI Library** | React                          | Component library     |
| **Styling**    | Tailwind CSS                   | Utility-first CSS     |
| **Hosting**    | Azure Kubernetes Service (AKS) | Containerized Next.js |

---

## Service Architecture

### Service Map

```
┌─────────────────────────────────────────────────────────────────┐
│                         External Clients                         │
│  (Browsers, Future Mobile Apps)                                  │
└────────────────┬────────────────┬───────────────┬────────────────┘
                 │                │               │
        ┌────────▼────────┐  ┌────▼──────┐  ┌────▼──────┐
        │ Landlord Portal │  │  Tenant   │  │  Market   │
        │   (Next.js)     │  │  Portal   │  │  Portal   │
        └────────┬────────┘  └────┬──────┘  └────┬──────┘
                 │                │               │
        ┌────────▼────────┐  ┌────▼──────┐  ┌────▼──────┐
        │ Landlord BFF    │  │ Tenant BFF│  │ Market BFF│
        │ (Session, JWT)  │  │           │  │           │
        └────────┬────────┘  └────┬──────┘  └────┬──────┘
                 │                │               │
                 └────────────────┼───────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        │              Internal Services Network              │
        │                                                     │
        │  ┌──────────┐  ┌───────────┐  ┌──────────────┐   │
        │  │ Identity │  │  Contact  │  │ Organization │   │
        │  │ Service  │  │  Service  │  │   Service    │   │
        │  └────┬─────┘  └─────┬─────┘  └──────┬───────┘   │
        │       │              │                │            │
        │  ┌────▼──────────────▼────────────────▼────────┐  │
        │  │          Permission Service                  │  │
        │  │   (Groups, Permissions, Authorization)       │  │
        │  └──────────────────┬──────────────────────────┘  │
        │                     │                              │
        │  ┌──────────┐  ┌───▼──────┐  ┌───────────┐      │
        │  │Property  │  │ Vacancy  │  │  Market   │      │
        │  │  Base    │  │ Service  │  │  Service  │      │
        │  └────┬─────┘  └─────┬────┘  └─────┬─────┘      │
        │       │              │              │             │
        │  ┌────▼────┐  ┌──────▼───┐  ┌──────▼────────┐   │
        │  │  Lease  │  │Inspection│  │  Maintenance  │   │
        │  │ Service │  │ Service  │  │    Service    │   │
        │  └─────────┘  └──────────┘  └───────────────┘   │
        │                                                   │
        │  ┌──────────────────────────────────────────┐   │
        │  │        Search Service (API + Worker)      │   │
        │  │         (Elasticsearch Indexing)          │   │
        │  └──────────────────────────────────────────┘   │
        │                                                   │
        │  ┌──────────────────────────────────────────┐   │
        │  │        Preferences Service                │   │
        │  │     (User UI Settings per Portal)         │   │
        │  └──────────────────────────────────────────┘   │
        └───────────────────────────────────────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │ Message Bus       │
                    │ (Kafka/ServiceBus)│
                    └───────────────────┘
```

### Service Inventory

#### Core Services (Authentication, Users, Permissions)

| Service          | Responsibility                                                       | Database          | Events Published                                                                            | Events Consumed                                |
|------------------|----------------------------------------------------------------------|-------------------|---------------------------------------------------------------------------------------------|------------------------------------------------|
| **Identity**     | User authentication, JWT generation, external logins (Google, Entra) | `identity_db`     | `UserCreated`, `UserActivated`, `UserDeactivated`                                           | -                                              |
| **Contact**      | Personal profiles, org-user profiles, GDPR deletion                  | `contact_db`      | `ContactCreated`, `ContactUpdated`, `ContactDeleted`                                        | `UserCreated`                                  |
| **Organization** | Organizations, companies, user-org membership                        | `organization_db` | `OrganizationCreated`, `CompanyCreated`, `UserAddedToOrganization`, `PermissionsRegistered` | -                                              |
| **Permission**   | Groups, permissions, authorization checks, cache                     | `permission_db`   | `GroupCreated`, `PermissionsChanged`                                                        | `OrganizationCreated`, `PermissionsRegistered` |
| **Preferences**  | User UI preferences (theme, language, columns) per portal/org        | `preferences_db`  | -                                                                                           | -                                              |

#### Domain Services (Property, Leasing, Operations)

| Service                 | Responsibility                                              | Database         | Events Published                                                       | Events Consumed                                            |
|-------------------------|-------------------------------------------------------------|------------------|------------------------------------------------------------------------|------------------------------------------------------------|
| **Property Base**       | Property, buildings, rental objects, rooms, components      | `property_db`    | `PropertyCreated`, `RentalObjectCreated`, `RentalObjectUpdated`        | -                                                          |
| **Rental Management**   | Vacancy periods, availability calculation, blocks           | `vacancy_db`     | `VacancyPeriodCreated`, `VacancyPeriodUpdated`, `PublicationRequested` | `RentalObjectCreated`, `LeaseActivated`, `LeaseTerminated` |
| **Market**              | Listings, applications, offers, view tracking               | `market_db`      | `ListingCreated`, `ApplicationSubmitted`, `OfferAccepted`              | `VacancyPeriodCreated`, `RentalObjectUpdated`              |
| **Lease**               | Lease agreements, approval, digital signatures, termination | `lease_db`       | `LeaseCreated`, `LeaseActivated`, `LeaseTerminated`                    | `OfferAccepted`                                            |
| **Dwelling Inspection** | Dwelling inspections, scheduling, assignment to inspectors  | `inspection_db`  | `InspectionCreated`, `InspectionCompleted`                             | `LeaseTerminated`                                          |
| **Maintenance**         | Fault notifications, work orders, repair tracking           | `maintenance_db` | `FaultNotificationCreated`, `WorkOrderCreated`                         | -                                                          |

#### Infrastructure Services

| Service    | Responsibility                           | Database               | Events Published | Events Consumed                                                           |
|------------|------------------------------------------|------------------------|------------------|---------------------------------------------------------------------------|
| **Search** | Elasticsearch indexing, autocomplete API | `search_db` (metadata) | -                | `RentalObjectCreated`, `ContactCreated`, `ListingCreated` (indexes to ES) |

#### BFF Services (Client Gateways)

| Service          | Responsibility                                                | Storage          | Exposed Externally |
|------------------|---------------------------------------------------------------|------------------|--------------------|
| **Landlord BFF** | Session management, JWT enrichment (org-scoped), YARP routing | Redis (sessions) | ✅ Yes (HTTPS)      |
| **Tenant BFF**   | Session management, user-scoped routing                       | Redis (sessions) | ✅ Yes (HTTPS)      |
| **Market BFF**   | Session management, public + user-scoped routing              | Redis (sessions) | ✅ Yes (HTTPS)      |

---

## Data Flow Patterns

## Pattern 1: User Registration (Choreographed - No Saga)

**Design Principle:** Avoid orchestration when possible. Let users complete their profile after authentication.

### Flow for Regular User:

```
1. User submits registration (POST /api/auth/register)
   ↓
2. Identity Service:
   - Creates user in database
   - Publishes Event: UserCreated(userId, email)
   - Returns success to client
   ↓
3. User logs in for first time
   ↓
4. BFF detects: User has no Contact (PersonalProfile)
   - Redirects to: /onboarding/create-profile
   ↓
5. User fills in personal information
   - POST /api/contacts (fullName, phone, etc.)
   ↓
6. Contact Service creates PersonalProfile
   - Links to userId
   - Publishes Event: ContactCreated
   ↓
7. Onboarding complete - user accesses system
```

### Flow for Employee with Invite Code:

```
1. Admin creates Contact first (PersonalProfile without userId)
   - POST /api/contacts { fullName, email, organizationId }
   ↓
2. Admin generates invite code
   - POST /api/contacts/{contactId}/invite
   - System sends email with invite link
   ↓
3. Employee registers with invite code
   - POST /api/auth/register { email, password, inviteCode: "ABC12345" }
   ↓
4. Identity Service:
   - Creates User account
   - Looks up Contact by invite code
   - Links User to existing Contact (Contact.UserId = User.Id)
   - Publishes Event: UserCreated(userId, email, linkedContactId)
   ↓
5. Employee logs in - already has Contact, no onboarding needed
   ↓
6. OrganizationUserProfile already exists → Employee sees organization
```

### Flow for Employee "Already Have Account":

```
1. Employee registers normally (regular flow above)
2. Employee logs in → creates basic Contact
3. Admin finds employee → initiates connection
   - POST /api/contacts/{contactId}/connect-user { userId }
4. Employee receives verification email → confirms
5. Contact updated with correct organization profile
```

**Benefits:**

- ✅ No saga complexity
- ✅ User controls profile creation
- ✅ Eventual consistency acceptable
- ✅ Failed registrations don't leave orphaned contacts

**Employee Organization Membership:**

- **Default Membership:** OrganizationUserProfile (not a Permission Group)
    - Created when admin adds Contact to organization
    - Simple many-to-many: Contact ↔ Organization
- **Permission Groups:** Optional, for specific roles
    - "Property Manager", "Lease Approver", etc.
    - Assigned separately after employee added to org

---

## Pattern 2: Property Publication to Market (Choreographed)

**Flow:**

```
1. Property Manager clicks "Publish" in Landlord Portal
   ↓
2. Rental Management Service:
   - Marks RentalObject as available
   - Publishes Event: PublicationRequested(rentalObjectId, availableFrom, availableTo)
   ↓
3. Rental Management Worker (internal):
   - Calculates vacancy periods (checks for lease overlaps, blocks)
   - Creates VacancyPeriod aggregate
   - Publishes Event: VacancyPeriodCreated
   ↓
4. Market Worker (listens to VacancyPeriodCreated):
   - Creates Listing aggregate (denormalizes rental object data)
   - Sets listing status: Published
   - Publishes Event: ListingCreated
   ↓
5. Search Worker (listens to ListingCreated):
   - Indexes listing in Elasticsearch
   - Listing now searchable on Market Portal
```

**No orchestrator needed** - each service reacts independently. Eventual consistency is acceptable (listing appears in
market within seconds).

---

## Pattern 3: Lease Termination → Dwelling Inspection Creation (Choreographed)

**Flow:**

```
1. Landlord terminates lease (POST /api/leases/{id}/terminate)
   ↓
2. Lease Service:
   - Updates lease status: Terminated
   - Publishes Event: LeaseTerminated(leaseId, rentalObjectId, terminationDate)
   ↓
3. Dwelling Inspection Worker (listens to LeaseTerminated):
   - Automatically creates DwellingInspection aggregate
   - Assigns to inspector (based on property rules)
   - Publishes Event: DwellingInspectionCreated
   ↓
4. Rental Management Worker (listens to LeaseTerminated):
   - Creates new VacancyPeriod (from termination date)
   - Publishes Event: VacancyPeriodCreated
   ↓
5. Market Worker (listens to VacancyPeriodCreated):
   - Creates new Listing (rental object available again)
```

**Parallel processing** - Dwelling Inspection and Rental Management services react independently, no coordination
needed.

---

## Pattern 4: Contact GDPR Deletion (Orchestrated Saga)

**Design Principle:** Use orchestration when multi-service validation is required before action.

**Flow:**

```
1. User requests data deletion (POST /api/contacts/delete-request)
   ↓
2. Contact Service starts GDPRDeletionSaga
   ↓
3. Saga Phase 1: Pre-Validation (parallel, read-only)
   - Call Lease Service: ValidateUserDeletion(userId)
     → Check for active leases → Block if active
   - Call Invoice Service: ValidateUserDeletion(userId)
     → Check for unpaid invoices → Block if unpaid
   - Call Maintenance Service: ValidateUserDeletion(userId)
     → Check for open work orders → OK (will anonymize)
   ↓
4. If ANY validation fails:
   - Saga state: ValidationFailed
   - Return error to user with blocking reasons
   - No compensations needed (no writes happened)
   ↓
5. If ALL validations pass, Saga Phase 2: Execution (sequential)
   - Command: AnonymizeContact → Contact Service
   - Command: DeactivateUser → Identity Service
   - Command: RemoveGroupMemberships → Permission Service
   - Command: AnonymizeMaintenanceRequests → Maintenance Service
   ↓
6. Saga state: Completed
```

### Pre-Validation Endpoints

**Exposed by each service:**

```http
POST /api/leases/validate-user-deletion
Body: { userId: "guid" }
Response: { 
  canDelete: false, 
  blockingReason: "User has 2 active leases" 
}

POST /api/invoices/validate-user-deletion
Body: { userId: "guid" }
Response: { 
  canDelete: false, 
  blockingReason: "User has 1 unpaid invoice" 
}

POST /api/maintenance/validate-user-deletion
Body: { userId: "guid" }
Response: { canDelete: true }
```

### Frontend Pre-Validation

**User Experience:**

```
User clicks "Delete My Account"
  ↓
Frontend calls validation endpoint:
  POST /api/contacts/validate-deletion-request { userId }
  ↓
BFF orchestrates validation calls (same as saga Phase 1)
  ↓
Returns result:
  - If can delete: Show "Are you sure?" confirmation
  - If blocked: Show reasons (e.g., "You have active leases")
  ↓
User confirms → Actual deletion saga starts
```

**Benefits:**

- ✅ Validate before any writes (no compensations needed)
- ✅ User gets immediate feedback (frontend validation)
- ✅ Consistent validation logic (same endpoints used by FE and saga)
- ✅ Audit trail of deletion attempts

### Generic Frontend Validation Pattern

**Any service can expose validation endpoints for frontend:**

**Example: Check Organization Name Uniqueness**

```http
POST /api/organizations/validate-name
Body: { name: "Acme Property Management" }
Response: { 
  isValid: false, 
  error: "Organization name already exists" 
}
```

**Example: Check Rental Object Number Uniqueness**

```http
POST /api/rental-objects/validate-number
Body: { buildingId: "guid", objectNumber: "101" }
Response: { 
  isValid: true 
}
```

**Implementation Pattern:**

```csharp
// Validation endpoint (read-only, fast)
[HttpPost("validate-name")]
public async Task<ValidationResult> ValidateName([FromBody] ValidateNameRequest request)
{
    var exists = await _repository.ExistsAsync(r => r.Name == request.Name);
    return new ValidationResult
    {
        IsValid = !exists,
        Error = exists ? "Organization name already exists" : null
    };
}

// Actual creation endpoint
[HttpPost]
public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationCommand command)
{
    // Command handler will validate again (defensive)
    // But frontend already validated, so this rarely fails
    var result = await _commandBus.SendAsync(command);
    return Ok(result);
}
```

**Benefits:**

- ✅ Immediate feedback to users (no waiting for command to fail)
- ✅ Reduces failed command attempts
- ✅ Better UX (red border on field immediately)
- ✅ Consistent with saga validation pattern

---
---

## Deployment Topology

### Local Development (Kind + Docker)

```
┌─────────────────────────────────────────────────────────┐
│                  Developer Laptop                        │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │           Kind Cluster (Docker)                 │    │
│  │                                                  │    │
│  │  ┌──────────────────────────────────────────┐  │    │
│  │  │  Traefik Ingress Controller              │  │    │
│  │  │  (landlord.dev, tenant.dev, market.dev) │  │
│  │  └────────┬─────────────────────────────────┘  │    │
│  │           │                                      │    │
│  │  ┌────────▼────────┐  ┌─────────────────────┐  │    │
│  │  │ BFFs (3 pods)   │  │ Services (11 pods)  │  │    │
│  │  └─────────────────┘  └─────────────────────┘  │    │
│  │                                                  │    │
│  │  ┌──────────────────────────────────────────┐  │    │
│  │  │ Workers (11 pods, separate deployments)  │  │    │
│  │  └──────────────────────────────────────────┘  │    │
│  │                                                  │    │
│  │  ┌──────────────────────────────────────────┐  │    │
│  │  │ Infrastructure:                           │  │    │
│  │  │ - Postgres (persistent volume)           │  │    │
│  │  │ - Redis                                   │  │    │
│  │  │ - Kafka + Zookeeper                       │  │    │
│  │  │ - Elasticsearch                           │  │    │
│  │  │ - SeaweedFS                                   │  │    │
│  │  │ - Jaeger, Prometheus, Loki, Grafana      │  │    │
│  │  └──────────────────────────────────────────┘  │    │
│  └──────────────────────────────────────────────────┘    │
│                                                          │
│  Docker Compose (Alternative Mode):                     │
│  - Infrastructure only (for Rider debugging)            │
│  - Full platform (attach debugger to containers)        │
└─────────────────────────────────────────────────────────┘
```

### Production (Azure Kubernetes Service (AKS))

```
┌─────────────────────────────────────────────────────────┐
│                     Azure Cloud                          │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │        Azure Kubernetes Service (AKS) Environment         │    │
│  │                                                  │    │
│  │  ┌──────────────────────────────────────────┐  │    │
│  │  │  Ingress (HTTPS, Custom Domains, TLS)   │  │    │
│  │  │  landlord.propertea.com                  │  │    │
│  │  │  tenant.propertea.com                    │  │    │
│  │  │  market.propertea.com                    │  │    │
│  │  └────────┬─────────────────────────────────┘  │    │
│  │           │                                      │    │
│  │  ┌────────▼────────┐  ┌─────────────────────┐  │    │
│  │  │ BFFs (3 apps,   │  │ Services (11 apps,  │  │    │
│  │  │ external=true)  │  │ external=false)     │  │    │
│  │  └─────────────────┘  └─────────────────────┘  │    │
│  │                                                  │    │
│  │  ┌──────────────────────────────────────────┐  │    │
│  │  │ Workers (11 apps, scale independently)   │  │    │
│  │  └──────────────────────────────────────────┘  │    │
│  └──────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │         Managed Azure Services                  │    │
│  │                                                  │    │
│  │  - Azure PostgreSQL Flexible Server            │    │
│  │  - Azure Cache for Redis                        │    │
│  │  - Azure Service Bus                            │    │
│  │  - Azure Blob Storage                           │    │
│  │  - Azure Key Vault                              │    │
│  │  - Azure Monitor / Application Insights         │    │
│  │  - Elasticsearch (Azure Marketplace or self-hosted) │
│  └──────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## Cross-Cutting Concerns

### Security

**Authentication:**

- BFFs: Session cookies (HTTP-only, secure, SameSite=Strict)
- Internal services: JWT validation (enriched with org context, permissions)
- External identity: Google OAuth, Azure Entra ID (optional, production)

**Authorization:**

- Permission-based (not role-based)
- Groups assigned at org or company scope
- BFF enriches JWT with permissions for current org
- Services validate permissions from JWT claims

**Service-to-Service:**

- MVP 1: Trusted network (no mTLS)
- Internal services not exposed externally
- Future: Service mesh with auto-mTLS (Istio/Linkerd on AKS)

### Observability

**Distributed Tracing:**

- OpenTelemetry instrumentation in all services
- Trace IDs propagated via HTTP headers
- Spans cover: HTTP requests, database queries, event publishing/consuming, saga steps

**Logging:**

- Structured JSON logs (Serilog)
- Log levels: Debug (dev), Information (prod), Warning/Error (always)
- Correlation IDs link logs across services

**Metrics:**

- Custom metrics: Event processing latency, saga duration, cache hit rate
- Standard metrics: HTTP request duration, database query time
- Local: Prometheus scraping, Grafana dashboards
- Production: Azure Monitor, Application Insights

**Dashboards:**

- Grafana dashboards for local development
- Azure Monitor workbooks for production

### Resilience

**Retry Policies:**

- Transient failures: Exponential backoff (.NET 9 Resilience APIs)
- HTTP calls: 3 retries with jitter
- Event processing: Dead-letter queue after 5 retries

**Circuit Breakers:**

- Protect downstream services from cascading failures
- Open circuit after 5 consecutive failures
- Half-open after 30 seconds

**Timeouts:**

- HTTP calls: 10 seconds default
- Database queries: 30 seconds
- Event processing: 60 seconds (heavy operations like ES indexing)

### Data Consistency

**Transactional Boundaries:**

- Aggregate root = transaction boundary
- Use EF Core transactions within aggregate
- Outbox pattern for publishing events (atomic with business transaction)

**Eventual Consistency:**

- Cross-service data updates via events
- Denormalized data refreshed on events (e.g., Market service caches rental object data)
- Periodic reconciliation jobs for drift detection (future)

---

## Design Decisions & Rationales

### Why Custom CQRS/Saga Libraries Instead of MediatR/MassTransit?

**Decision:** Build `ProperTea.ProperCqrs` and `ProperTea.ProperSagas` instead of using established libraries.

**Rationale:**

- ✅ **Educational Value:** Deep understanding of patterns by implementing them
- ✅ **Avoid Commercial Licenses:** MassTransit/MediatR have commercial restrictions
- ✅ **Control:** Customize to exact needs (no feature bloat)
- ✅ **Lightweight:** Simpler codebase, easier debugging

**Trade-off:** More maintenance, fewer community resources. Acceptable for learning-focused project.

### Why Separate Worker Projects?

**Decision:** Each service has separate API and Worker projects (e.g., `Identity.Service` + `Identity.Worker`).

**Rationale:**

- ✅ **Independent Scaling:** Scale workers separately from APIs (Search worker needs 10 replicas, API needs 2)
- ✅ **Clear Separation:** API handles HTTP, Worker handles events (no mixed concerns)
- ✅ **Educational:** Learn worker patterns across all services
- ✅ **Deployment Flexibility:** Deploy workers as separate containers, functions, or sidecars

**Trade-off:** More projects to manage. Mitigated by shared domain libraries.

### Why BFF Pattern Instead of Direct API Gateway?

**Decision:** Dedicated BFF per client type (Landlord, Tenant, Market) instead of single API gateway.

**Rationale:**

- ✅ **Session Management:** BFFs handle session cookies (secure for web, different for future mobile)
- ✅ **Client-Specific Logic:** Landlord BFF enriches JWT with org permissions, Tenant BFF doesn't
- ✅ **Security Isolation:** BFFs are trusted boundary, internal services never exposed
- ✅ **Aggregation:** BFFs can aggregate responses (future feature)

**Trade-off:** More services to deploy. Worth it for security and flexibility.

### Why Kafka Locally, Service Bus in Production?

**Decision:** Use Kafka for local/AKS, Azure Service Bus for ACA production.

**Rationale:**

- ✅ **Portability:** Kafka works everywhere (docker, k8s, cloud-agnostic)
- ✅ **Learning:** Industry-standard event streaming (valuable skill)
- ✅ **Production Pragmatism:** Service Bus is managed, no operational overhead in ACA
- ✅ **Abstraction:** `ProperIntegrationEvents` library swaps providers via config

**Trade-off:** Different behaviors (Kafka topics vs Service Bus queues). Mitigated by abstraction.

### Why Kind for Local k8s Instead of k3s?

**Decision:** Use Kind (Kubernetes in Docker) for local pre-production testing.

**Rationale:**

- ✅ **Self-Contained:** Entire cluster in Docker, easy cleanup (`kind delete cluster`)
- ✅ **Multi-Node:** Test HA scenarios (3-node cluster)
- ✅ **CI/CD Friendly:** GitHub Actions has built-in Kind support
- ✅ **No System Installation:** Unlike k3s (requires sudo)

**Note:** Docker Compose still used for daily "inner loop" debugging (faster iteration).

---

## Next Steps

**For Developers:**

1. Read `01-authentication-authorization.md` for auth flow details
2. Read `02-service-specifications.md` for individual service designs
3. Read `09-local-development.md` for setup instructions
4. Start with Phase 1 services (see `11-implementation-roadmap.md`)

**For AI Assistants:**

- This document provides system-wide context
- Reference service-specific docs for implementation details
- Follow patterns described here for consistency across services

---

**Document Version History:**

| Version | Date       | Changes                                                |
|---------|------------|--------------------------------------------------------|
| 1.0.0   | 2025-10-22 | Initial architecture approved for MVP 1 implementation |

