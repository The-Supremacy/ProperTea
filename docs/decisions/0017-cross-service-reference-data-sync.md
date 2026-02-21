# Cross-Service Reference Data Synchronization

**Status**: Accepted
**Date**: 2025-07-21
**Deciders**: Architecture

## Context

Services in ProperTea need reference data from other services to support local queries and UI rendering. For example, the Property service displays a company name alongside each property, requiring a local copy of company data.

Previously, the Property service consumed integration events (`ICompanyCreated`, `ICompanyUpdated`, `ICompanyDeleted`) from the Company service via RabbitMQ to maintain a local `CompanyReference` document. This works for steady-state operation but has two gaps:

1. **Initial population**: When a new consumer service starts for the first time (or a new reference data concern is added), the RabbitMQ queue contains no historical messages. The local reference store starts empty.
2. **Disaster recovery**: If the consumer's database is rebuilt or the queue is purged, there is no mechanism to repopulate reference data without replaying source-side domain events (which is not the consumer's responsibility).

We evaluated several approaches:

- **Lazy loading via HTTP on read** - Adds latency to every query, creates runtime coupling, and makes the system fragile under network partitions.
- **Synchronous HTTP calls for writes** - Tight coupling, no decoupling benefit.
- **Event replay from source service** - Violates service boundaries; each service owns its own event store.
- **HTTP snapshot for seeding + fat events for steady state** - Decoupled steady state, simple one-time seed for bootstrapping.

## Decision

We adopt a **two-channel approach**: fat integration events for real-time updates, plus a simple HTTP snapshot endpoint for one-time seeding.

### Channel 1: Fat Integration Events (steady state)

Integration events carry the full entity state (not deltas). Consumers extract only the fields they need. This is the existing pattern, unchanged.

Example: `ICompanyUpdated` carries `CompanyId`, `OrganizationId`, `Code`, `Name`, `UpdatedAt`.

### Channel 2: HTTP Snapshot Endpoint (initial seed / recovery)

Each source service exposes one internal GET endpoint returning all entities across all tenants:

```
GET /internal/companies/snapshot
```

Returns a flat list of snapshot items. No authentication (internal network only). No pagination (reference data sets are small enough to fit in a single response).

Each consumer service exposes a POST trigger to run the seed:

```
POST /internal/references/companies/seed
```

This is a manual, one-time operation. No automatic startup, no `IHostedService`.

### Timestamp Guards (idempotency)

Every handler that writes reference data (event handlers and seed handler) applies a timestamp guard:

```csharp
var existing = await session.LoadAsync<CompanyReference>(id);
if (existing != null && existing.LastUpdatedAt >= incomingTimestamp)
    return; // skip stale write
```

This makes all writes idempotent and order-independent. Combined with RabbitMQ's durable queues (provisioned before the seed runs), this eliminates race conditions between the seed and concurrent event processing without any orchestration.

### Aspire Wiring

The Property service gets a `WithReference(companyService)` in the AppHost to enable Aspire service discovery for the HTTP client. The named HttpClient uses `https+http://company` as the base address.

## Consequences

### Positive

- **Decoupled steady state**: No runtime HTTP dependency. Events flow asynchronously via RabbitMQ.
- **Simple bootstrapping**: One manual POST to seed reference data. No complex orchestration.
- **Idempotent writes**: Timestamp guards mean events and seed data can arrive in any order, be processed multiple times, and still converge to the correct state.
- **No schema coupling**: Consumers define their own reference document shape (`CompanyReference`), storing only the fields they need.

### Negative

- **First service-to-service HTTP dependency**: The Property service now has a direct HTTP reference to the Company service, breaking the previous pattern of RabbitMQ-only cross-service communication. This is limited to internal/seed operations only.
- **Manual seed step**: Operators must remember to run the seed POST when bootstrapping a new environment. This could be documented in runbooks.

### Risks / Mitigation

- **Snapshot endpoint returns stale data** -> Timestamp guards ensure stale snapshot items are harmlessly skipped. Real-time events will have already applied newer state.
- **Large reference data sets** -> Current scale is small (companies per tenant). If this grows significantly, add pagination to the snapshot endpoint.
- **Seed endpoint called multiple times** -> Fully idempotent due to timestamp guards. Safe to re-run.
