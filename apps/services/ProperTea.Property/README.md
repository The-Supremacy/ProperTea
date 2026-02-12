# ProperTea.Property

**Responsibility**: Manages the physical reality of real estate assets - properties and their constituent units. Separated from commercial concerns (rentals, market listings).

## Overview
The Property service owns the "Physical Existence" layer of real estate management. It tracks physical attributes, building structures, and inventory of properties and units. According to ADR 0001, Property and Unit are separate aggregate roots to enable independent updates without performance locking.

## Key Concepts

### Aggregates
- **PropertyAggregate**: Represents the management context (building, estate, complex)
  - Owned by a Company (required `CompanyId`)
  - Physical attributes: Address, PropertyType, SquareFootage, RoomCount
  - Soft deletion support
- **UnitAggregate**: Represents a distinct physical space within a Property
  - Required `PropertyId` reference to parent Property
  - Physical attributes: UnitNumber, Floor, SquareFootage, RoomCount
  - Can be updated independently from Property (ADR 0001 performance optimization)

### Property-Unit Relationship (ADR 0001)
- Property and Unit are **separate aggregate roots** (not parent-child)
- Unit holds `PropertyId` FK reference
- Private houses: 1 Property + 1 Unit (maintains architectural uniformity)
- Deleting Property triggers eventual consistency cascade to delete Units

### Event Sourcing
- **Property Events**: `property.created.v1`, `property.details-updated.v1`, `property.deleted.v1`
- **Unit Events**: `unit.created.v1`, `unit.details-updated.v1`, `unit.deleted.v1`
- **Persistence**: Inline snapshot projections with Marten
- **Indexes**: PropertyId, CompanyId, UnitNumber, Status

### Multi-Tenancy
Properties and Units are **strictly tenant-scoped** using `ITenanted`:
- `TenantId` = ZITADEL organization ID (ADR 0010)
- All operations automatically scoped via Marten tenancy
- Wolverine handlers use `InvokeForTenantAsync`

### Integration Events
**Published**:
- `properties.created.v1` → Rental Service, Work Order Service
- `properties.updated.v1` → Downstream services
- `properties.deleted.v1` → Triggers Unit cascade deletion
- `units.created.v1` → Rental Service (rentable status tracking)
- `units.updated.v1` → Downstream services
- `units.deleted.v1` → Rental Service cleanup

**Subscribed**:
- `companies.deleted.v1` → Cascade delete properties (eventual consistency)
- `properties.deleted.v1` → Cascade delete units (handled internally)

## Service Boundaries

### In Scope (Physical Reality)
- Physical property attributes (address, type, structure)
- Physical unit characteristics (floor, rooms, square footage)
- Building inventory and asset tracking
- Property-Unit relationships

### Out of Scope (Commercial Reality)
- Rental schedules and contracts → **Rental Service**
- Rentable status and blocks → **Rental Service**
- Base financials and lost rent calculations → **Rental Service**
- Market listings and applicant funnel → **Market Service** (future)
- Maintenance tasks and inspections → **Work Order Service** (future)

## Technical Stack
- **.NET 10**: Target framework
- **Marten**: Event sourcing, inline snapshot projections
- **Wolverine**: CQRS handlers, messaging, HTTP endpoints
- **RabbitMQ**: Integration event pub/sub
- **JWT Authentication**: Tenant-scoped operations using ZITADEL organization ID

## Domain Exceptions
Structured domain exceptions:
- `BusinessViolationException(PROPERTY_NAME_REQUIRED)` - Name is empty
- `BusinessViolationException(PROPERTY_COMPANY_REQUIRED)` - CompanyId is missing
- `BusinessViolationException(PROPERTY_ALREADY_DELETED)` - Operation on deleted property
- `NotFoundException(PROPERTY_NOT_FOUND)` - Property doesn't exist
- `BusinessViolationException(UNIT_PROPERTY_REQUIRED)` - PropertyId is missing
- `BusinessViolationException(UNIT_NUMBER_REQUIRED)` - UnitNumber is empty
- `NotFoundException(UNIT_NOT_FOUND)` - Unit doesn't exist

## Configuration

### Required Environment Variables
```bash
OIDC__Authority=<ZITADEL_URL>
OIDC__Issuer=<ZITADEL_ISSUER>
OIDC__Audience=<API_AUDIENCE>
ConnectionStrings__ProperTeaDb=<PostgreSQL_Connection>
RabbitMQ__Host=<RabbitMQ_Host>
```

## Development

### Running Locally
From solution root with Aspire:
```bash
dotnet run --project orchestration/ProperTea.AppHost
```

Access Aspire Dashboard: `https://localhost:17285`

## Architecture Decisions
- [ADR 0001: Property-Unit Aggregates](../../../docs/decisions/0001-property-unit-aggregates.md) - Separation pattern
- [ADR 0002: Rental Service Split](../../../docs/decisions/0002-rental-service-split.md) - Service boundaries
- [ADR 0007: Organization Multi-Tenancy](../../../docs/decisions/0007-organization-multi-tenancy.md) - Multi-tenancy
- [ADR 0010: Direct Tenant ID Mapping](../../../docs/decisions/0010-direct-tenant-id-mapping.md) - ZITADEL org ID

## Related Services
- **Company Service**: Properties reference company as owner via `CompanyId`
- **Rental Service**: Consumes property/unit creation events to track rentable inventory
- **Work Order Service**: References properties/units for maintenance tasks (future)
- **Landlord BFF**: Exposes property/unit endpoints to frontend
