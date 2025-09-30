# Multitenancy: Tenant Model and Enforcement

## Overview
ProperTea uses shared-table multitenancy with an OrganizationId discriminator. Companies live within an Organization. Users can belong to multiple organizations with different group memberships and permissions. Some permissions may be scoped to specific companies inside an organization.

## Tenant Context Resolution
- API path always includes organization: `/api/v0/organization/{orgId}/...`
- Gateway rejects requests if orgId not in the user's orgs[]
- Gateway forwards `X-Org-Id` header and, when applicable, `X-Company-Id`; services validate consistency
- Services apply global query filters on OrganizationId

## Data Model Patterns
- All tenant-aware tables include `OrganizationId`
- Foreign keys include OrganizationId to prevent cross-tenant references
- Optional indexes on (OrganizationId, …) for hot queries
- For company-scoped entities, include `CompanyId` and enforce org/company consistency in service logic

## Isolation Enforcement
- Gateway: early rejection of cross-org access
- Services:
  - Local checks and EF global filters by OrganizationId
  - Company-scoped permission enforcement (verify target companyId is allowed per token)
- Database: constraints to avoid cross-tenant references
- Cache: tenant-scoped keys, e.g., `{service}:{orgId}:{key}`

## Companies Within Organizations
- Company is scoped to a single Organization
- Service business logic should validate that any companyId belongs to current org (enforced at service layer)
- Cross-org checks at gateway can be added later if needed

## Feature Flags
- Flags are org-scoped and can be refined per group
- Effective features for a user = union of all groups within org
- UI convenience: selecting a feature toggles all its functions

## Migration Paths
- Schema-per-tenant: introduce model cache key factory and dynamic schema
- Database-per-tenant: per-tenant connection string resolution
- Hybrid: give large tenants stronger isolation without changing app code

## Testing and Quality Gates
- Unit/integration tests must assert org scoping and company-scoped enforcement where applicable
- Contract tests include org (and company where present) in paths
- CI policy to ensure new tables include OrganizationId (and CompanyId where applicable)
