# Implementation Guide: Canonical External Identifiers (ADR 0014)

This document provides the file-by-file implementation plan for ADR 0014. It eliminates the dual-ID pattern for Organizations and Users, making ZITADEL identifiers the canonical external identity.

## Scope

**In scope**: Renaming fields, changing types from `Guid` to `string` for Organization/User references in contracts and integration events, removing `Guid.Parse(session.TenantId)` calls, updating API responses and endpoints.

**Out of scope**: Marten stream identity configuration (stays `AsGuid`), aggregate `Guid Id` field (stays, becomes private to service), Marten event store data migration (separate task).

## Guiding Rules

1. `Guid Id` on aggregates is **private** — never appears in integration events, API responses, or cross-service contracts.
2. `OrganizationId` is always `string` (ZITADEL org ID). In handlers, use `session.TenantId` directly — never `Guid.Parse()`.
3. `UserId` is always `string` (ZITADEL `sub` claim). Renamed from `ExternalUserId`.
4. `OrganizationId` on aggregates is renamed from `ExternalOrganizationId`. `UserId` is renamed from `ExternalUserId`.
5. For Company, Property, Unit — their own `Id` remains `Guid` (no ZITADEL counterpart). Only their `OrganizationId` event fields change from `Guid` to `string`.

## Change Plan by Layer

### 1. Contracts (`shared/ProperTea.Contracts/Events/`)

These are the source of truth for cross-service communication. Change all `Guid OrganizationId` to `string OrganizationId` and `Guid ProfileId` to `string UserId`.

#### `OrganizationIntegrationEvents.cs`

```csharp
// BEFORE
public interface IOrganizationRegistered
{
    public Guid OrganizationId { get; }
    public string Name { get; }
    public string ExternalOrganizationId { get; }
    public DateTimeOffset RegisteredAt { get; }
}

// AFTER
public interface IOrganizationRegistered
{
    public string OrganizationId { get; }       // ZITADEL org ID (was ExternalOrganizationId)
    public string Name { get; }
    public DateTimeOffset RegisteredAt { get; }
    // ExternalOrganizationId removed — OrganizationId IS the ZITADEL ID now
}
```

Apply same pattern to `IOrganizationDeactivated`, `IOrganizationActivated`, `IOrganizationUpdated`, `IOrganizationDomainVerified`: change `Guid OrganizationId` → `string OrganizationId`.

#### `CompanyIntegrationEvents.cs`

```csharp
// BEFORE
public interface ICompanyCreated
{
    Guid CompanyId { get; }
    Guid OrganizationId { get; }    // ← Guid, populated via Guid.Parse(session.TenantId)
    ...
}

// AFTER
public interface ICompanyCreated
{
    Guid CompanyId { get; }
    string OrganizationId { get; }  // ← string, directly session.TenantId
    ...
}
```

Apply to `ICompanyCreated`, `ICompanyUpdated`, `ICompanyDeleted`.

#### `PropertyIntegrationEvents.cs`

Same change: `Guid OrganizationId` → `string OrganizationId` on all property and unit event contracts (`IPropertyCreated`, `IPropertyUpdated`, `IPropertyDeleted`, `IUnitCreated`, `IUnitUpdated`, `IUnitDeleted`).

#### `UserProfileIntegrationEvents.cs`

```csharp
// BEFORE
public interface IUserProfileCreated
{
    Guid ProfileId { get; }
    string ExternalUserId { get; }
    DateTimeOffset CreatedAt { get; }
}

// AFTER
public interface IUserProfileCreated
{
    string UserId { get; }          // ZITADEL sub claim (was ExternalUserId)
    DateTimeOffset CreatedAt { get; }
    // ProfileId (Guid) removed — private to User service
}
```

---

### 2. Organization Service (`apps/services/ProperTea.Organization/`)

#### `OrganizationAggregate.cs`

```csharp
// BEFORE
public string? ExternalOrganizationId { get; set; }

// AFTER
public string? OrganizationId { get; set; }
```

`Guid Id` stays (Marten stream key, private).

#### `OrganizationEvents.cs`

```csharp
// BEFORE
public record ExternalOrganizationCreated(Guid OrganizationId, string ExternalOrganizationId);

// AFTER — rename event and field
public record OrganizationLinked(Guid StreamId, string OrganizationId);
// StreamId = Marten stream Guid (internal only)
// OrganizationId = ZITADEL org ID
```

Update other events: where they reference `Guid OrganizationId` as the stream key, rename to `Guid StreamId` to avoid confusion with the canonical `string OrganizationId`.

Alternative (simpler): keep event field names as-is internally since domain events are private to the service. Only integration events and API responses need the new naming. Choose based on team preference — the critical part is that **no domain event `OrganizationId` leaks outside the service**.

#### `OrganizationIntegrationEvents.cs` (publisher side)

```csharp
// BEFORE
[MessageIdentity("organizations.registered.v1")]
public record OrganizationRegistered(
    Guid OrganizationId,
    string Name,
    string ExternalOrganizationId,
    DateTimeOffset RegisteredAt) : IOrganizationRegistered;

// AFTER
[MessageIdentity("organizations.registered.v2")]
public record OrganizationRegistered(
    string OrganizationId,          // ZITADEL org ID
    string Name,
    DateTimeOffset RegisteredAt) : IOrganizationRegistered;
```

#### `RegisterOrganization.cs` (handler)

```csharp
// BEFORE
var orgId = Guid.NewGuid();
// ... creates ZITADEL org, gets externalOrgId ...
await bus.PublishAsync(new OrganizationRegistered(orgId, name, externalOrgId, timestamp));

// AFTER
var streamId = Guid.NewGuid();      // Marten stream key (private)
// ... creates ZITADEL org, gets organizationId (string) ...
await bus.PublishAsync(new OrganizationRegistered(organizationId, name, timestamp));
```

#### `OrganizationEndpoints.cs`

```csharp
// BEFORE
GET /organizations/{id}                           // Guid
GET /organizations/external/{externalOrgId}       // string

// AFTER
GET /organizations/{organizationId}               // string (ZITADEL ID, canonical)
// Remove the /external/ endpoint — there is only one ID now
```

Update `OrganizationResponse` to drop `ExternalOrganizationId` field. The response's main identifier becomes `string OrganizationId`.

#### `GetOrganizationByExternalIdHandler.cs`

Rename to `GetOrganizationHandler.cs`. Query by `OrganizationId` (string) instead of by `ExternalOrganizationId`. Drop the old `GetOrganizationHandler` that queried by `Guid Id`.

#### `OrganizationMartenConfiguration.cs`

Rename the unique index from `ExternalOrganizationId` to `OrganizationId`.

---

### 3. User Service (`apps/services/ProperTea.User/`)

#### `UserProfileAggregate.cs`

```csharp
// BEFORE
public string ExternalUserId { get; set; }

// AFTER
public string UserId { get; set; }
```

`Guid Id` stays (Marten stream key, private).

#### `UserProfileEvents.cs`

```csharp
// BEFORE
public record Created(Guid ProfileId, string ExternalUserId, DateTimeOffset CreatedAt);

// AFTER
public record Created(Guid StreamId, string UserId, DateTimeOffset CreatedAt);
```

Same pattern: domain events are internal, but rename for clarity.

#### `UserProfileIntegrationEvents.cs` (publisher side)

```csharp
// BEFORE
public class UserProfileCreatedEvent(Guid profileId, string externalUserId, ...)

// AFTER
[MessageIdentity("users.profile-created.v2")]
public class UserProfileCreatedEvent(string userId, DateTimeOffset createdAt) : IUserProfileCreated;
```

#### `UserProfileEndpoints.cs`

```csharp
// BEFORE
GET /users/me                                  // returns Guid Id + ExternalUserId
GET /users/external/{externalUserId}           // string

// AFTER
GET /users/me                                  // returns string UserId (no Guid)
GET /users/{userId}                            // string (ZITADEL sub, canonical)
// Remove the /external/ route — there is only one ID now
```

#### `UserProfileMartenConfiguration.cs`

Rename unique index from `ExternalUserId` to `UserId`.

#### Subscriber: `External/OrganizationIntegrationEvents.cs`

Update the subscriber-side event record to match the new v2 contract:

```csharp
// BEFORE
public record OrganizationRegistered(
    Guid OrganizationId, string Name, string Slug,
    string ExternalOrganizationId, ...);

// AFTER
public record OrganizationRegistered(
    string OrganizationId, string Name, ...);
```

#### Projections (`UserProfileListView`, `UserProfileDetailsView`)

Replace `ExternalUserId` with `UserId` in projection fields.

---

### 4. Company Service (`apps/services/ProperTea.Company/`)

#### `CompanyIntegrationEvents.cs` (publisher side)

```csharp
// BEFORE
public class CompanyCreated : ICompanyCreated
{
    public Guid OrganizationId { get; set; }  // populated via Guid.Parse(session.TenantId)
}

// AFTER
public class CompanyCreated : ICompanyCreated
{
    public string OrganizationId { get; set; } // directly session.TenantId
}
```

Apply to `CompanyCreated`, `CompanyUpdated`, `CompanyDeleted`.

#### `CreateCompanyHandler.cs`, `UpdateCompanyHandler.cs`, `DeleteCompanyHandler.cs`

```csharp
// BEFORE
var organizationId = Guid.Parse(session.TenantId);

// AFTER
var organizationId = session.TenantId;
```

#### Subscriber: Organization events handler

Update subscriber-side record to match new v2 contract (same as User service subscriber change).

---

### 5. Property Service (`apps/services/ProperTea.Property/`)

#### `PropertyIntegrationEvents.cs` (publisher side)

```csharp
// BEFORE
public class PropertyCreated : IPropertyCreated
{
    public Guid OrganizationId { get; set; }
}

// AFTER
public class PropertyCreated : IPropertyCreated
{
    public string OrganizationId { get; set; }
}
```

Apply to all 6 event classes (`PropertyCreated`, `PropertyUpdated`, `PropertyDeleted`, `UnitCreated`, `UnitUpdated`, `UnitDeleted`).

#### All handlers with `Guid.Parse(session.TenantId)`

6 files to update:
- `CreatePropertyHandler.cs` (line 48)
- `UpdatePropertyHandler.cs` (line 52)
- `DeletePropertyHandler.cs` (line 26)
- `CreateUnitHandler.cs` (line 71)
- `UpdateUnitHandler.cs` (line 58)
- `DeleteUnitHandler.cs` (line 26)

```csharp
// BEFORE
var organizationId = Guid.Parse(session.TenantId);

// AFTER
var organizationId = session.TenantId;
```

#### Company event subscribers (`CompanyCreatedHandler.cs`, `CompanyUpdatedHandler.cs`)

```csharp
// BEFORE
TenantId = message.OrganizationId.ToString()

// AFTER
TenantId = message.OrganizationId    // already a string now
```

Update the subscriber-side company event records: `Guid OrganizationId` → `string OrganizationId`.

---

### 6. BFF (`apps/portals/landlord/bff/`)

#### `SessionEndpoints.cs`

```csharp
// BEFORE
public record SessionDto(
    string ExternalUserId,
    string ExternalOrganizationId, ...);

// AFTER
public record SessionDto(
    string UserId,
    string OrganizationId, ...);
```

#### `OrganizationDtos.cs`

```csharp
// BEFORE
public record OrganizationDto(Guid Id, string? ExternalOrganizationId, ...);
public record RegisterOrganizationResponse(Guid OrganizationId);

// AFTER
public record OrganizationDto(string OrganizationId, ...);
public record RegisterOrganizationResponse(string OrganizationId);
```

Remove `Guid Id` from all organization DTOs. The `OrganizationId` (string) is the only identifier.

#### `UserDtos.cs`

```csharp
// BEFORE
public record UserProfileDto(Guid Id, string ExternalUserId, ...);

// AFTER
public record UserProfileDto(string UserId, ...);
```

#### `UserClient.cs`

```csharp
// BEFORE
var response = await client.GetAsync($"/users/external/{externalUserId}");

// AFTER
var response = await client.GetAsync($"/users/{userId}");
```

#### `OrganizationClient.cs`

Update API paths: `/organizations/external/{id}` → `/organizations/{id}`.

---

### 7. Frontend (`apps/portals/landlord/web/`)

#### `session.service.ts`

```typescript
// BEFORE
export interface SessionContext {
  externalOrganizationId: string;
  externalUserId: string;
}

// AFTER
export interface SessionContext {
  organizationId: string;
  userId: string;
}
```

Update all references across the Angular app.

#### `organization.models.ts`

```typescript
// BEFORE
export interface OrganizationDetailResponse {
  id: string;                        // internal Guid
  externalOrganizationId?: string;   // ZITADEL
}

// AFTER
export interface OrganizationDetailResponse {
  organizationId: string;            // ZITADEL ID, canonical
}
```

#### `user.service.ts`

Update field references from `externalUserId` to `userId`. Update API paths.

#### All components referencing `externalOrganizationId` or `externalUserId`

Search-and-replace across the Angular app. Key files:
- `organization-details.component.ts`
- `organization-registration.component.ts`
- Any component reading `sessionContext.externalOrganizationId`

---

### 8. Shared Infrastructure (`shared/ProperTea.Infrastructure.Common/`)

#### `IOrganizationIdProvider` / `OrganizationIdProvider`

No functional change needed — already returns `string`. But review any comments or variable names referencing "external".

---

### 9. Documentation Updates

| File | Change |
|---|---|
| `docs/decisions/0009-user-identity-strategy.md` | Add `**Status**: Superseded by [0014](0014-canonical-external-identifiers.md)` |
| `docs/architecture.md` | Update "Key Patterns > Multi-Tenancy" section to remove dual-ID references |
| `docs/event-catalog.md` | Update event versions (v1 → v2) for organization, company, property, unit, and user events |
| `docs/dev/multi-tenancy-flow.md` | Update to reflect `string OrganizationId` everywhere |
| `docs/domain.md` | No change needed (doesn't reference ID types) |
| `.github/instructions/dotnet.instructions.md` | Update if it references External*Id patterns |
| `.github/instructions/contracts.instructions.md` | Update ID type guidance |

---

## Implementation Order

Execute in this order to minimize broken builds:

1. **Contracts** — Change interface types. Everything breaks (expected).
2. **Publisher-side integration events** — Update implementations in Organization, Company, Property, User services.
3. **Handlers** — Replace `Guid.Parse(session.TenantId)` with direct `session.TenantId` usage.
4. **Aggregates** — Rename `ExternalOrganizationId` → `OrganizationId`, `ExternalUserId` → `UserId`.
5. **Aggregate Apply methods** — Update to use new event/field names.
6. **Marten configuration** — Update index definitions.
7. **Subscriber-side event records** — Update records in consuming services.
8. **Endpoints & responses** — Update route parameters, response DTOs.
9. **BFF** — Update DTOs, clients, endpoints.
10. **Frontend** — Update TypeScript models, services, components.
11. **Documentation** — Update ADR 0009 status, architecture docs, event catalog.
12. **Build & test** — `dotnet build ProperTea.slnx` must pass with zero warnings.

## Event Versioning Strategy

For each changed event:

1. Update the `[MessageIdentity]` to v2 (e.g., `organizations.registered.v2`).
2. Since this is pre-production, we can do a hard cutover — no need to maintain v1 compatibility.
3. If we were in production, we would: publish both v1 and v2 during transition, let subscribers migrate individually, then retire v1.

## Marten Event Store Migration

Since we are in development, the simplest approach is:

1. Drop and recreate databases (Marten handles schema creation on startup).
2. If preserving data is needed: write a one-time migration that renames `ExternalOrganizationId` → `OrganizationId` and `ExternalUserId` → `UserId` in stored event JSON payloads (Marten stores events as JSONB in PostgreSQL).
