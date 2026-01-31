# ProperTea.Organization

**Responsibility**: The "Tenant Master" service. Orchestrates headless organization registration and manages organization lifecycles.

## Overview
This service integrates with ZITADEL to create organizations and their initial admin users atomically. It maintains the `OrganizationAggregate` as the local source of truth for organization metadata and publishes integration events that other services consume to initialize tenant-specific data.

## Technical Stack
- **.NET 10**: Target framework
- **Marten**: Event sourcing for `OrganizationAggregate`, document storage for projections
- **Wolverine**: CQRS handlers and messaging infrastructure
- **RabbitMQ**: Integration event publishing
- **ZITADEL v2 APIs**: External organization and user management

## Key Concepts

### Registration Flow
The registration process is handled by a **Reliable Handler** pattern:
1. Receive `RegisterOrganization` command
2. Call ZITADEL API to create organization + admin user (atomic operation)
3. Persist `OrganizationAggregate` with events
4. Publish `organizations.registered.v1` integration event
5. Other services react to create tenant-specific resources

### Event Sourcing
- **Aggregate**: `OrganizationAggregate`
- **Events**: `Created`, `DetailsUpdated`, `Suspended`, `Reactivated`
- **Persistence**: Inline snapshot projections with Marten

### Multi-Tenancy
While this service manages tenant metadata, it is **not multi-tenanted** itself (no `ITenanted`). Each organization record is accessible globally within the service but contains the ZITADEL organization ID which becomes the `TenantId` for other services.

## Configuration

### Required Environment Variables
```bash
OIDC__Authority=<ZITADEL_URL>
OIDC__Audience=<API_AUDIENCE>
ConnectionStrings__ProperTeaDb=<PostgreSQL_Connection>
RabbitMQ__Host=<RabbitMQ_Host>
ZITADEL__ApiUrl=<ZITADEL_API_URL>
ZITADEL__ServiceUserId=<Service_Account_ID>
ZITADEL__PrivateKey=<PAT_or_JWT_Key>
```

## Development

### Running Locally
From solution root with Aspire:
```bash
dotnet run --project orchestration/ProperTea.AppHost
```

## Architecture Decisions
- [0003-headless-onboarding.md](../../../docs/decisions/0003-headless-onboarding.md) - ZITADEL registration strategy
- [0007-organization-multi-tenancy.md](../../../docs/decisions/0007-organization-multi-tenancy.md) - Multi-tenancy foundation
- [0010-direct-tenant-id-mapping.md](../../../docs/decisions/0010-direct-tenant-id-mapping.md) - ZITADEL org ID as TenantId

## Related Services
- **Company Service**: Creates default company on `organizations.registered.v1`
- **User Service**: Creates user profile on `organizations.registered.v1`
- All other services initialize tenant-specific resources on registration event
