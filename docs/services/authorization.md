# Authorization Service

## Purpose
Provide the effective permission model for a user within an organization and support rare centralized checks if needed.

## Responsibilities
- Maintain permission definitions grouped by service domain
- Manage user groups and their assigned permissions (per organization)
- Support permission scoping at organization-wide or specific company level
- Compute effective permissions for user+org (union of groups)
- Expose read endpoints for permission models
- Optional: provide lightweight check endpoint for critical rechecks

## API
- `GET /auth/user/{userId}/org/{orgId}/permissions-model`
  - Returns:
    - `permissionsByService` + `roles`
    - `permissionScopes` mapping for company-scoped permissions:
      - Example: `{ "property": { "edit-properties": { "scopeType": "company", "companyIds": ["comp-1"] } } }`
  - Caching: may be cached in Redis under userId (per-user model)
- (Optional later) `POST /auth/check`
  - Input: userId, orgId, action, resource (optional), companyId (optional)
  - Output: allowed/denied + reason

## Data Model (conceptual)
- Permissions(service, action)
- Groups(orgId, name) → GroupPermissions(groupId, permissionId, scopeType, companyIds)
- Memberships(orgId, userId, groupId)
- EmergencyGrants(orgId, userId, permissions[], scopeType, companyIds[], expiresAt, reason, ticket)

## Multitenancy
- All records are scoped by OrganizationId where applicable
- Support team permissions are special-cased with restricted PII access

## Security
- Only trusted gateway/service tokens can call APIs
- Audit all changes to groups and permissions
- Emergency grants auto-expire after 24 hours

## Observability
- Metrics: permission model cache hit/miss, computation time
- Traces: model computation and DB queries
- Logs: changes to permissions/groups audited

## Roadmap
- Event-driven invalidation on permission changes
- Permission versioning with hash in internal token
- Optional OPA integration if policies grow complex
