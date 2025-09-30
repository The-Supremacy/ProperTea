# ADR-002: Multitenancy Strategy

## Status
Accepted

## Context

ProperTea needs to support multiple organizations (tenants) with proper data isolation while maintaining cost efficiency and development simplicity. Each organization may have multiple companies within it, and users can belong to multiple organizations with different permissions.

Requirements:
- Clear data separation between organizations
- Users can access multiple organizations
- Companies exist within organizations as business entities
- Support for future migration to stronger isolation models
- Simple implementation with Entity Framework Core
- Cost-effective for small to medium tenants

Multitenancy options evaluated:
1. Database-per-tenant: Maximum isolation, high operational overhead
2. Schema-per-tenant: Good isolation, moderate complexity
3. Shared-table with discriminator: Minimal overhead, simplest implementation

## Decision

Primary approach: Shared-table with tenant discriminator.

- Data model: All tenant-aware entities include `OrganizationId` discriminator column.
- Enforcement: Global query filters in Entity Framework Core.
- URL structure: `/api/v0/organization/{organizationId}/service/endpoint`.
- Gateway validation: API Gateway validates organizationId against user's accessible organizations (early reject).
- Propagation: Gateway forwards a canonical `X-Org-Id` header to services.

## Entity Framework Guidance
- All tenant-aware entities must include `OrganizationId`.
- Configure global query filters to automatically scope queries by `OrganizationId`.
- Prefer composite indexes starting with `OrganizationId` for hot paths.
- Where possible, include `OrganizationId` in foreign keys to prevent cross-tenant relations.

## Tenant Resolution
- Source: Organization ID comes from the URL path.
- Validation: Gateway ensures user has access to the requested organization (based on the user’s orgs[]).
- Propagation: `X-Org-Id` header forwarded to backend services.
- Context: Services resolve tenant context per request from header/path and validate consistency.

## Organization Hierarchy
- Organizations: Top-level tenant boundary for billing and isolation.
- Companies: Business entities within an organization (address, bank accounts, legal details).
- Users: May belong to multiple organizations with different permissions per org.

## Consequences

Positive:
- Simple EF Core implementation; minimal plumbing
- Cost effective and easy to operate for many tenants
- Fast development and testing in a single database
- Easy portability to Azure SQL (avoid Postgres-only features)

Negative:
- Application-level isolation; higher risk of coding mistakes causing leakage
- Complex per-tenant export and lifecycle operations
- Potential noisy-neighbor effects in very large multi-tenant databases

## Migration Path

- Schema-per-tenant:
  - Introduce EF Core model cache key per tenant and dynamic schema selection.
  - Keep table shapes identical; move large tenants incrementally.
- Database-per-tenant:
  - Move to per-tenant connection strings and context factory.
  - Useful for very large tenants or strong isolation needs.
- Hybrid:
  - Small/medium tenants stay shared; large tenants get schema/db isolation.

## Risk Mitigations
- Global query filters to enforce tenant scoping
- Gateway validation for early rejection
- Database constraints where possible to avoid cross-tenant references
- Comprehensive tests for tenant isolation
- Audit logging with tenant context on critical operations

## Implementation Guidelines
1. All tenant-aware entities include `OrganizationId`.
2. All API endpoints for tenant-aware domains include `/organization/{organizationId}/...`.
3. Gateway verifies that `organizationId` ∈ user.orgs[] and forwards `X-Org-Id`.
4. Services validate that header/path orgs match and apply global filters.
5. Cross-tenant operations must be explicit, reviewed, and audited.
