# Use ZITADEL Organization ID Directly as Tenant ID

**Status**: Superseded by [0018-keycloak-adoption.md](0018-keycloak-adoption.md)
**Date**: 2026-01-31
**Deciders**: oxface, GitHub Copilot

## Context
ProperTea uses Marten for multi-tenancy with the `ITenanted` interface requiring a `TenantId` property. ZITADEL provides organization context via `org_id` claim in JWT tokens.

Initial designs considered maintaining an internal `OrganizationId` (Guid) and mapping it to/from ZITADEL's organization ID, which would require:
- Additional database lookup on every request to translate ZITADEL org ID → internal OrganizationId
- Separate mapping table or aggregate property to maintain bidirectional relationship
- Risk of synchronization issues between systems
- Performance overhead from extra database query per request

The system must support thousands of requests per second at scale, making per-request lookups prohibitively expensive.

## Decision
Use ZITADEL's organization ID (string) directly as the `TenantId` in all multi-tenanted aggregates and documents.

**Implementation**:
- `ITenanted.TenantId` contains ZITADEL organization ID directly
- No internal OrganizationId property or mapping layer
- `IOrganizationIdProvider` extracts org_id from JWT token claims
- Marten tenant scoping uses ZITADEL org ID directly: `session.ForTenant(orgId)`
- All aggregates use `string TenantId` property matching ZITADEL format

## Consequences

### Positive
* **Zero lookup overhead**: No database queries needed to translate tenant identifiers
* **Simplicity**: Single source of truth for organization identity (ZITADEL)
* **Performance**: Supports high-throughput scenarios without bottlenecks
* **Consistency**: Token claims directly match database tenant scoping
* **Reduced complexity**: No synchronization logic between internal and external IDs
* **YAGNI**: Avoids premature abstraction that may never be needed

### Negative
* **Coupling to ZITADEL**: Tenant ID format tied to external IdP
* **Migration complexity**: Changing IdP providers requires data migration of all TenantId values
* **String vs Guid**: Uses strings instead of more compact Guid format (minor storage overhead)

### Risks / Mitigation
* **IdP Lock-in**: If we need to migrate away from ZITADEL → Mitigate with one-time data migration script. This is acceptable as IdP migration is rare and typically involves data transformation regardless of identifier strategy.
* **Identifier format constraints**: External system controls format → Mitigate by validating org_id claim format at authentication boundary. ZITADEL uses consistent string format.

## Alternatives Considered
1. **Internal OrganizationId with mapping**: Rejected due to performance overhead of per-request lookups
2. **Composite key (Internal + External)**: Rejected as overly complex with no clear benefit
3. **Cache layer for mapping**: Adds complexity and cache invalidation concerns; still slower than no mapping

## Related Decisions
- [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md): Established Marten conjoined tenancy pattern
- [0009-user-identity-strategy.md](0009-user-identity-strategy.md): Uses ZITADEL user ID directly for similar reasons
