# Migration Guide: Extract Building as Separate Aggregate

**ADR**: 0015
**Scope**: Property Service backend, BFF, Frontend
**Prerequisite**: Read ADR 0015, ADR 0001 (Unit separation precedent), ADR 0013 (query-time joins)

## Overview

Building is currently a child entity of PropertyAggregate with events stored in the Property event stream. This guide extracts it into its own aggregate root following the same patterns used throughout the codebase.

**Reference implementations to follow:**
- `Features/Properties/` (aggregate with field-level events, closest match)
- `Features/Units/` (separate aggregate with `PropertyId` FK, same parent relationship)

---

## Phase 1: Create Building Feature (New Files)

All new files go under `apps/services/ProperTea.Property/Features/Buildings/`.

### 1.1 Create `BuildingEvents.cs`

```csharp
namespace ProperTea.Property.Features.Buildings;

public static class BuildingEvents
{
    public record Created(
        Guid BuildingId,
        Guid PropertyId,
        string Code,
        string Name,
        DateTimeOffset CreatedAt);

    public record CodeUpdated(Guid BuildingId, string Code);
    public record NameUpdated(Guid BuildingId, string Name);

    public record Deleted(Guid BuildingId, DateTimeOffset DeletedAt);
}
```

Use field-level update events (matching Property's `CodeUpdated`/`NameUpdated`/`AddressUpdated` pattern, NOT the old monolithic `Updated` event).

### 1.2 Create `BuildingAggregate.cs`

```csharp
public class BuildingAggregate : IRevisioned, ITenanted
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Status CurrentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    // Factory + mutation methods following Decider pattern
    // Apply methods for each event
    // Validation: ValidateCode (same rules as PropertyAggregate.ValidateBuildingCode)

    public enum Status { Active = 1, Deleted = 2 }
}
```

Key points:
- `PropertyId` is set on creation and immutable (same as `UnitAggregate.PropertyId`)
- Validate `PropertyId != Guid.Empty` in `Create()`
- Code/Name validation rules are identical to the existing `PropertyAggregate.ValidateBuildingCode` and building name check

### 1.3 Create `ErrorCodes.cs`

```csharp
namespace ProperTea.Property.Features.Buildings;

public static class BuildingErrorCodes
{
    public const string BUILDING_NOT_FOUND = "BUILDING_NOT_FOUND";
    public const string BUILDING_NAME_REQUIRED = "BUILDING_NAME_REQUIRED";
    public const string BUILDING_CODE_REQUIRED = "BUILDING_CODE_REQUIRED";
    public const string BUILDING_CODE_TOO_LONG = "BUILDING_CODE_TOO_LONG";
    public const string BUILDING_CODE_ALREADY_EXISTS = "BUILDING_CODE_ALREADY_EXISTS";
    public const string BUILDING_ALREADY_DELETED = "BUILDING_ALREADY_DELETED";
    public const string BUILDING_PROPERTY_REQUIRED = "BUILDING_PROPERTY_REQUIRED";
}
```

Keep the same error code string values as the existing `PropertyErrorCodes.BUILDING_*` constants so frontend translations continue to work without changes.

### 1.4 Create `Configuration/BuildingMartenConfiguration.cs`

```csharp
public static void ConfigureBuildingMarten(this StoreOptions opts)
{
    _ = opts.Projections.Snapshot<BuildingAggregate>(SnapshotLifecycle.Inline);

    _ = opts.Schema.For<BuildingAggregate>()
        .Index(x => x.Code)
        .Index(x => x.Name)
        .Index(x => x.PropertyId)
        .Index(x => x.CurrentStatus);

    opts.Events.MapEventType<BuildingEvents.Created>("building.created.v1");
    opts.Events.MapEventType<BuildingEvents.CodeUpdated>("building.code-updated.v1");
    opts.Events.MapEventType<BuildingEvents.NameUpdated>("building.name-updated.v1");
    opts.Events.MapEventType<BuildingEvents.Deleted>("building.deleted.v1");
}
```

Use NEW event type names (`building.*`), NOT the old `property.building-*` names. The old events remain mapped in PropertyMartenConfiguration for backward compatibility with existing streams.

### 1.5 Create `Configuration/BuildingFeatureExtensions.cs`

```csharp
public static IServiceCollection AddBuildingFeature(this IServiceCollection services)
{
    return services;
}
```

### 1.6 Create Lifecycle Handlers

Create these files under `Features/Buildings/Lifecycle/`:

| File | Command/Query | Notes |
|---|---|---|
| `CreateBuildingHandler.cs` | `CreateBuilding(Guid PropertyId, string Code, string Name)` | Validate Property exists via `session.LoadAsync<PropertyAggregate>`. Check code uniqueness via `session.Query<BuildingAggregate>().Where(b => b.PropertyId == ... && b.Code == ... && b.CurrentStatus == Active)`. Start new stream keyed by BuildingId. Return `Guid`. |
| `UpdateBuildingHandler.cs` | `UpdateBuilding(Guid BuildingId, string? Code, string? Name)` | Load BuildingAggregate. Emit `CodeUpdated`/`NameUpdated` per changed field (follow `UpdateCompanyHandler` pattern). Check code uniqueness within same PropertyId excluding self. |
| `DeleteBuildingHandler.cs` | `DeleteBuilding(Guid BuildingId)` | Load BuildingAggregate. Emit `Deleted` event. |
| `GetBuildingHandler.cs` | `GetBuilding(Guid BuildingId)` | Load and return single Building. |
| `ListBuildingsHandler.cs` | `ListBuildings(BuildingFilters, PaginationQuery, SortQuery)` | Query `BuildingAggregate` directly with pagination. Filter by `PropertyId` (required), `Code`, `Name`. Resolve PropertyName via query-time join if needed. |
| `SelectBuildingsHandler.cs` | `SelectBuildings(Guid PropertyId)` | Query `BuildingAggregate` where `PropertyId` matches and status is Active. Return `List<BuildingSelectItem>`. |
| `GetBuildingAuditLogHandler.cs` | `GetBuildingAuditLogQuery(Guid BuildingId)` | Same pattern as `GetPropertyAuditLogHandler`. Map `Created`, `CodeUpdated`, `NameUpdated`, `Deleted` to audit entries with old/new value tracking. |

**Important for `CreateBuildingHandler`**: The Property existence check should use `session.LoadAsync<PropertyAggregate>` (loads the inline snapshot), NOT `session.Events.AggregateStreamAsync` (replays all events). Check that `property.CurrentStatus != Deleted`.

**Important for `ListBuildingsHandler`**: This replaces the current `ListBuildingsHandler` in `Features/Properties/Buildings/`. The old one loads the entire PropertyAggregate. The new one queries `BuildingAggregate` directly via Marten LINQ (like `ListPropertiesHandler` queries `PropertyAggregate`). Add pagination support.

### 1.7 Create `BuildingEndpoints.cs`

Keep the same URL structure (`/properties/{propertyId}/buildings/*`) since the route reflects domain hierarchy, not aggregate boundary:

```csharp
public static class BuildingEndpoints
{
    [WolverinePost("/properties/{propertyId}/buildings")]
    [Authorize]
    public static async Task<IResult> CreateBuilding(
        Guid propertyId,
        BuildingRequest request,
        IMessageBus bus,
        IOrganizationIdProvider orgProvider)
    {
        var tenantId = orgProvider.GetOrganizationId()
            ?? throw new UnauthorizedAccessException("Organization ID required");

        var buildingId = await bus.InvokeForTenantAsync<Guid>(
            tenantId,
            new CreateBuilding(propertyId, request.Code, request.Name));

        return Results.Created(
            $"/properties/{propertyId}/buildings/{buildingId}",
            new { Id = buildingId });
    }

    // ... other endpoints follow same pattern
    // For update/delete/get, use BuildingId as the primary identifier
    // For list/select, filter by PropertyId
}
```

**URL conventions:**
- `POST /properties/{propertyId}/buildings` - create (needs PropertyId)
- `GET /properties/{propertyId}/buildings` - list (filter by PropertyId)
- `GET /properties/{propertyId}/buildings/select` - select dropdown
- `GET /buildings/{buildingId}` - get single (BuildingId is sufficient)
- `PUT /buildings/{buildingId}` - update
- `DELETE /buildings/{buildingId}` - delete
- `GET /buildings/{buildingId}/audit-log` - audit log

Note: GET/PUT/DELETE by ID don't need `propertyId` in the URL since `BuildingId` is globally unique. But the `POST` and list endpoints do need it to scope the context. Follow whichever pattern the team prefers for consistency. If you prefer uniform nesting, keep `propertyId` in all routes.

---

## Phase 2: Remove Building from PropertyAggregate

### 2.1 Edit `PropertyEvents.cs`

Remove these three event records:
```csharp
// DELETE THESE:
public record BuildingAdded(Guid PropertyId, Guid BuildingId, string Code, string Name);
public record BuildingUpdated(Guid PropertyId, Guid BuildingId, string Code, string Name);
public record BuildingRemoved(Guid PropertyId, Guid BuildingId);
```

### 2.2 Edit `PropertyAggregate.cs`

Remove ALL Building-related code:

1. Delete the `Building` class (lines 7-13)
2. Delete the `Buildings` property: `public List<Building> Buildings { get; set; } = [];`
3. Delete factory methods: `AddBuilding()`, `UpdateBuilding()`, `RemoveBuilding()`
4. Delete Apply methods: `Apply(BuildingAdded)`, `Apply(BuildingUpdated)`, `Apply(BuildingRemoved)`
5. Delete `ValidateBuildingCode()` helper method

### 2.3 Edit `PropertyMartenConfiguration.cs`

Remove the three Building event type mappings:
```csharp
// DELETE THESE:
opts.Events.MapEventType<PropertyEvents.BuildingAdded>("property.building-added.v1");
opts.Events.MapEventType<PropertyEvents.BuildingUpdated>("property.building-updated.v1");
opts.Events.MapEventType<PropertyEvents.BuildingRemoved>("property.building-removed.v1");
```

**Note on existing data**: Marten will encounter `property.building-added.v1` events in existing Property streams. Since no CLR type maps to them anymore, they are ignored during inline snapshot aggregation. This is safe - the Property aggregate simply won't process them, and the inline snapshot projection will skip unknown events.

### 2.4 Edit `ErrorCodes.cs` (`PropertyErrorCodes`)

Remove:
```csharp
// DELETE THESE (they move to BuildingErrorCodes):
public const string BUILDING_NOT_FOUND = "BUILDING_NOT_FOUND";
public const string BUILDING_NAME_REQUIRED = "BUILDING_NAME_REQUIRED";
public const string BUILDING_CODE_REQUIRED = "BUILDING_CODE_REQUIRED";
public const string BUILDING_CODE_TOO_LONG = "BUILDING_CODE_TOO_LONG";
public const string BUILDING_CODE_ALREADY_EXISTS = "BUILDING_CODE_ALREADY_EXISTS";
```

### 2.5 Delete old Building handlers directory

Delete the entire `Features/Properties/Buildings/` directory:
- `AddBuildingHandler.cs`
- `UpdateBuildingHandler.cs`
- `RemoveBuildingHandler.cs`
- `ListBuildingsHandler.cs`
- `SelectBuildingsHandler.cs`

### 2.6 Edit `PropertyEndpoints.cs`

Remove ALL Building endpoint methods:
- `ListBuildings`
- `SelectBuildings`
- `AddBuilding`
- `UpdateBuilding`
- `RemoveBuilding`

Remove the `BuildingRequest` record and the `using ProperTea.Property.Features.Properties.Buildings;` import.

### 2.7 Edit `GetPropertyAuditLog.cs`

Remove Building-related audit data and event handling:

1. Delete from `PropertyAuditEventData`:
   - `BuildingAddedData`
   - `BuildingUpdatedData`
   - `BuildingRemovedData`

2. Delete from the `switch` expression in the handler (the `evt.Data switch` block):
   - `BuildingAdded` case
   - `BuildingUpdated` case
   - `BuildingRemoved` case

3. Delete from the state rebuild switch:
   - `case BuildingAdded`
   - `case BuildingUpdated`
   - `case BuildingRemoved`

### 2.8 Edit `GetPropertyHandler.cs`

Remove `Buildings` from the response:

```csharp
// BEFORE:
public record PropertyResponse(Guid Id, Guid CompanyId, string Code, string Name, string Address,
    List<BuildingResponse> Buildings, string Status, DateTimeOffset CreatedAt);
public record BuildingResponse(Guid Id, string Code, string Name);

// AFTER:
public record PropertyResponse(Guid Id, Guid CompanyId, string Code, string Name, string Address,
    string Status, DateTimeOffset CreatedAt);
```

Remove the `BuildingResponse` record.
Remove the Buildings projection from the handler's return statement.

### 2.9 Edit `ListPropertiesHandler.cs`

Replace `BuildingCount` with a query-time join (ADR 0013 pattern):

```csharp
// After fetching paginated properties, resolve building counts:
var propertyIds = properties.Select(p => p.Id).ToList();

// Query BuildingAggregate for counts per PropertyId
var buildingCounts = await session.Query<BuildingAggregate>()
    .Where(b => b.PropertyId.In(propertyIds) && b.CurrentStatus == BuildingAggregate.Status.Active)
    .GroupBy(b => b.PropertyId)
    .Select(g => new { PropertyId = g.Key, Count = g.Count() })
    .ToListAsync();
var buildingCountLookup = buildingCounts.ToDictionary(x => x.PropertyId, x => x.Count);
```

Then use `buildingCountLookup.GetValueOrDefault(p.Id, 0)` in the projection. If Marten's LINQ provider doesn't support `GroupBy` + `Count`, use a raw SQL query or do two queries:

```csharp
// Fallback: fetch all active buildings for the page's property IDs
var buildings = await session.Query<BuildingAggregate>()
    .Where(b => b.PropertyId.In(propertyIds) && b.CurrentStatus == BuildingAggregate.Status.Active)
    .ToListAsync();
var buildingCountLookup = buildings
    .GroupBy(b => b.PropertyId)
    .ToDictionary(g => g.Key, g => g.Count());
```

---

## Phase 3: Update Unit's Building Validation

### 3.1 Edit `CreateUnitHandler.cs`

Replace the Building validation that loads PropertyAggregate:

```csharp
// BEFORE (loads entire Property to check Buildings list):
if (command.BuildingId.HasValue)
{
    var building = property.Buildings
        .FirstOrDefault(b => b.Id == command.BuildingId.Value && !b.IsRemoved)
        ?? throw new NotFoundException(...);
}

// AFTER (queries BuildingAggregate directly):
if (command.BuildingId.HasValue)
{
    var building = await session.LoadAsync<BuildingAggregate>(command.BuildingId.Value);
    if (building is null || building.CurrentStatus == BuildingAggregate.Status.Deleted)
        throw new NotFoundException(
            BuildingErrorCodes.BUILDING_NOT_FOUND,
            "Building",
            command.BuildingId.Value);

    if (building.PropertyId != command.PropertyId)
        throw new BusinessViolationException(
            BuildingErrorCodes.BUILDING_NOT_FOUND,
            "Building does not belong to this property");
}
```

Add `using ProperTea.Property.Features.Buildings;` to the file.

---

## Phase 4: Wire Up Registration

### 4.1 Edit `Configuration/MartenConfiguration.cs`

Add the Building Marten configuration call:

```csharp
opts.ConfigureCompanyReferenceMarten();
opts.ConfigurePropertyMarten();
opts.ConfigureBuildingMarten();  // ADD THIS
opts.ConfigureUnitMarten();
```

Add `using ProperTea.Property.Features.Buildings.Configuration;`

### 4.2 Edit `Program.cs`

Add the Building feature registration:

```csharp
builder.Services.AddPropertyFeature();
builder.Services.AddBuildingFeature();  // ADD THIS
builder.Services.AddUnitFeature();
```

Add `using ProperTea.Property.Features.Buildings.Configuration;`

---

## Phase 5: BFF Layer

### 5.1 Edit `PropertyClient.cs`

Remove from `PropertyDetailResponse`:
- `IReadOnlyList<BuildingResponse> Buildings`
- The `BuildingResponse` record

Keep `BuildingCount` in `PropertyListItem` (now resolved via query-time join in the backend).

Remove `BuildingCount` from `PropertyListItem` if the backend no longer returns it (depends on implementation choice; the backend `ListPropertiesHandler` still returns it via the query-time join, so keep it).

### 5.2 No BFF Building endpoints exist

The BFF currently has no Building-specific endpoints. When a Building list-view is needed on the frontend, add BFF endpoints at that time following the same pass-through pattern as Properties.

---

## Phase 6: Frontend

### 6.1 Edit `property.models.ts`

Remove `BuildingResponse` interface and `buildings` from `PropertyDetailResponse`:

```typescript
// BEFORE:
export interface PropertyDetailResponse {
  id: string;
  companyId: string;
  code: string;
  name: string;
  address: string;
  buildings: BuildingResponse[];
  status: string;
  createdAt: Date;
}

// AFTER:
export interface PropertyDetailResponse {
  id: string;
  companyId: string;
  code: string;
  name: string;
  address: string;
  status: string;
  createdAt: Date;
}
```

Delete the `BuildingResponse` interface.

### 6.2 Edit `property-audit-log.component.ts`

Remove the three Building cases from `formatEventData()`:
- `case 'buildingadded':`
- `case 'buildingupdated':`
- `case 'buildingremoved':`

These events will no longer appear in the Property audit log for new data. Historical events may still be shown from the Property's event stream (the backend handler will encounter unknown events and format them with the `default` case).

### 6.3 Translations

Keep all existing Building translations in place. They will be used by the future Building list-view and Building audit log.

The `properties.events.buildingadded`, `properties.events.buildingupdated`, `properties.events.buildingremoved` translations can be kept for historical audit entries or moved to a new `buildings.events.*` section later when the Building UI is built.

---

## Phase 7: Documentation Updates

### 7.1 Edit `docs/domain.md`

Change Building's definition:

```markdown
| **Building** (Aggregate Root) | A physical structure within a Property. Has a unique Code per Property. | Property Service |
```

Remove "(Child Entity of Property)" and "Stored in the Property event stream."

### 7.2 Edit `docs/event-catalog.md`

If Building events are listed, update them to reflect the new event type names (`building.created.v1`, etc.) and note they are in their own stream.

---

## Verification Checklist

After all changes:

- [ ] `dotnet build apps/services/ProperTea.Property/ProperTea.Property.csproj` succeeds with zero errors
- [ ] `dotnet build apps/portals/landlord/bff/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj` succeeds
- [ ] `cd apps/portals/landlord/web/landlord-portal && npm run build` succeeds
- [ ] No references to `PropertyEvents.BuildingAdded`, `PropertyEvents.BuildingUpdated`, or `PropertyEvents.BuildingRemoved` remain
- [ ] No references to `PropertyAggregate.Buildings` remain (except in `CreateUnitHandler` which should now use `BuildingAggregate` instead)
- [ ] `BuildingAggregate` has inline snapshot, indexes on `Code`, `Name`, `PropertyId`, `CurrentStatus`
- [ ] Building code uniqueness is enforced per PropertyId in `CreateBuildingHandler` and `UpdateBuildingHandler`
- [ ] `CreateUnitHandler` validates `BuildingId` against `BuildingAggregate`
- [ ] Property audit log no longer processes Building events
- [ ] `PropertyErrorCodes.BUILDING_*` constants are removed; `BuildingErrorCodes.BUILDING_*` exist with same string values

## File Summary

### New files (create)
| Path | Purpose |
|---|---|
| `Features/Buildings/BuildingAggregate.cs` | Aggregate root |
| `Features/Buildings/BuildingEvents.cs` | Domain events |
| `Features/Buildings/BuildingEndpoints.cs` | HTTP endpoints |
| `Features/Buildings/ErrorCodes.cs` | Error code constants |
| `Features/Buildings/Configuration/BuildingMartenConfiguration.cs` | Marten setup |
| `Features/Buildings/Configuration/BuildingFeatureExtensions.cs` | DI registration |
| `Features/Buildings/Lifecycle/CreateBuildingHandler.cs` | Create command |
| `Features/Buildings/Lifecycle/UpdateBuildingHandler.cs` | Update command (field-level) |
| `Features/Buildings/Lifecycle/DeleteBuildingHandler.cs` | Delete command |
| `Features/Buildings/Lifecycle/GetBuildingHandler.cs` | Get query |
| `Features/Buildings/Lifecycle/ListBuildingsHandler.cs` | List query (paginated) |
| `Features/Buildings/Lifecycle/SelectBuildingsHandler.cs` | Select dropdown query |
| `Features/Buildings/Lifecycle/GetBuildingAuditLogHandler.cs` | Audit log query |

### Files to edit
| Path | Change |
|---|---|
| `Features/Properties/PropertyEvents.cs` | Remove 3 Building event records |
| `Features/Properties/PropertyAggregate.cs` | Remove Building class, list, commands, Apply methods, validation |
| `Features/Properties/PropertyEndpoints.cs` | Remove 5 Building endpoints, BuildingRequest, using statement |
| `Features/Properties/ErrorCodes.cs` | Remove 5 BUILDING_* constants |
| `Features/Properties/Configuration/PropertyMartenConfiguration.cs` | Remove 3 event mappings |
| `Features/Properties/Lifecycle/GetPropertyAuditLog.cs` | Remove 3 Building audit records, 6 switch cases |
| `Features/Properties/Lifecycle/GetPropertyHandler.cs` | Remove Buildings from response, delete BuildingResponse |
| `Features/Properties/Lifecycle/ListPropertiesHandler.cs` | Replace `Buildings.Count` with query-time join |
| `Features/Units/Lifecycle/CreateUnitHandler.cs` | Change Building validation to use BuildingAggregate |
| `Configuration/MartenConfiguration.cs` | Add `opts.ConfigureBuildingMarten()` |
| `Program.cs` | Add `builder.Services.AddBuildingFeature()` |
| BFF `PropertyClient.cs` | Remove Buildings from PropertyDetailResponse |
| Frontend `property.models.ts` | Remove BuildingResponse, buildings from detail |
| Frontend `property-audit-log.component.ts` | Remove 3 Building event cases |
| `docs/domain.md` | Update Building definition |

### Files to delete
| Path | Reason |
|---|---|
| `Features/Properties/Buildings/AddBuildingHandler.cs` | Replaced by `Features/Buildings/Lifecycle/CreateBuildingHandler.cs` |
| `Features/Properties/Buildings/UpdateBuildingHandler.cs` | Replaced |
| `Features/Properties/Buildings/RemoveBuildingHandler.cs` | Replaced |
| `Features/Properties/Buildings/ListBuildingsHandler.cs` | Replaced |
| `Features/Properties/Buildings/SelectBuildingsHandler.cs` | Replaced |
