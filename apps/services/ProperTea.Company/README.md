# ProperTea.Company

**Responsibility**: Manages legal business entities (Companies) that own properties and conduct business operations within an organization.

## Overview
The Company service maintains company records representing legal entities (LLCs, Corporations, etc.) that own real estate assets. Each organization can have multiple companies. This service uses event sourcing and automatically creates a default company when a new organization is registered.

## Technical Stack
- **.NET 10**: Target framework
- **Marten**: Event sourcing for `CompanyAggregate`, inline snapshot projections
- **Wolverine**: CQRS handlers, messaging infrastructure, and HTTP endpoints
- **RabbitMQ**: Integration event publishing and consumption
- **JWT Authentication**: Tenant-scoped operations using ZITADEL organization ID

## Key Concepts

### Event Sourcing
- **Aggregate**: `CompanyAggregate`
- **Events**: `Created`, `NameUpdated`, `Deleted`
- **Persistence**: Inline snapshot projections with Marten
- **Naming**: `company.created.v1`, `company.name-updated.v1`, `company.deleted.v1`

### Multi-Tenancy
Companies are **strictly tenant-scoped** using `ITenanted`:
- `TenantId` = ZITADEL organization ID (no internal mapping)
- All operations automatically scoped to organization via Marten tenancy
- Wolverine handlers use `InvokeForTenantAsync` for automatic tenant context

### Soft Deletion
Companies use soft delete pattern:
- `CurrentStatus`: `Active` | `Deleted`
- `DeletedAt`: Timestamp when deleted
- Deleted companies remain in event store but are filtered from queries

## Domain Exceptions
The service uses structured domain exceptions:
- `BusinessViolationException(COMPANY_NAME_REQUIRED)` - Name is empty
- `BusinessViolationException(COMPANY_ALREADY_DELETED)` - Operation on deleted company
- `NotFoundException(COMPANY_NOT_FOUND)` - Company doesn't exist

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

## Future Enhancements
- Add company address and contact information
- Add tax ID and legal entity type
- Support company hierarchies (subsidiaries)
- Add company-specific branding/settings

## Architecture Decisions
- [0010-direct-tenant-id-mapping.md](../../../docs/decisions/0010-direct-tenant-id-mapping.md) - ZITADEL org ID as TenantId
- [0007-organization-multi-tenancy.md](../../../docs/decisions/0007-organization-multi-tenancy.md) - Multi-tenancy implementation

## Related Services
- **Organization Service**: Publishes registration events that trigger default company creation
- **Property Service**: Properties will reference company as owner (future)
- **Landlord BFF**: Exposes company endpoints to frontend
