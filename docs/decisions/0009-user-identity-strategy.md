# ADR 0009: Identity Strategy - Users and Organizations

## Status
Accepted

## Context

Both Users and Organizations are **domain entities** with business logic beyond authentication. They need:
- Internal domain IDs (Guid) for relationships and stable references
- External IDs (string) for bridging from authentication tokens (ZITADEL)

### Token Flow
1. **User Identification**: Services read standard JWT `sub` claim directly (portable across IdPs)
2. **Organization Identification**: BFF extracts ZITADEL-specific claim (`urn:zitadel:iam:org:id`) and forwards as `X-Organization-Id` header (abstraction layer)

This design keeps downstream services decoupled from ZITADEL - they work with standard OIDC claims and generic headers.

## Decision

**Keep Both IDs Pattern for Both Users and Organizations**

Each aggregate has:
- `Guid Id` - Internal domain identifier (primary key for event streams)
- `string External*Id` - ZITADEL identifier (unique indexed for queries)

**Naming Convention:**
- Organizations: `Id` and `ExternalOrganizationId`
- Users: `Id` and `ExternalUserId`

**Never use ambiguous names:** `UserId` or `OrgId` (unclear which ID is meant)

## Rationale

### Why Domain IDs (Guid)?
- Clean foreign key relationships between entities
- Stable integration event references (external systems don't need ZITADEL IDs)
- Event sourcing best practice (Marten optimized for Guid streams)
- Domain identity separate from authentication identity

### Why External IDs (string)?
- Direct mapping from JWT tokens (no extra lookup needed)
- All token-based queries use these
- Multi-IdP ready (can map Auth0/Okta later without breaking domain)
- Bridge from authentication to domain

### Why Both?
Organizations have subscriptions, billing, properties. Users have preferences, activity, relationships. These are domain concepts that exist independent of ZITADEL - the external ID is just how we find them from auth tokens.

## Access Patterns

**REST Endpoints:**
- `/users/{guid}` - Direct access by internal ID (admin, relationships)
- `/users/me` - Current user from JWT `sub` claim → queries by ExternalUserId
- `/organizations/{guid}` - Direct access by internal ID (cross-org queries)
- `/organizations/me` - Current org from `X-Organization-Id` header → queries by ExternalOrganizationId

**Query Usage:**
- Token-based user queries: Read `sub` claim from JWT (standard OIDC, no BFF abstraction needed)
- Token-based org queries: Read `X-Organization-Id` header (BFF abstracts ZITADEL-specific claim)
- Direct queries (admin, reports): Query by internal Id
- Relationships/FKs: Use internal Id

**Integration Events:**
- Use internal Guid IDs for stable cross-service references
- Include external IDs optionally for ZITADEL sync if needed

## Preferences

Single global preference set per user (no per-application field):
- Simpler model and queries
- BFF can filter/trim fields for each frontend as needed
- Unique index: `ExternalUserId`

## Consequences

### Positive
- Clean separation: Domain identity vs Auth identity
- Flexible for multi-IdP future without migration
- Relationships use Guids (not ZITADEL strings)
- `/me` endpoints provide clean auth-to-domain bridge
- Services decoupled from ZITADEL (use standard `sub` claim and generic headers)

### Negative
- Two ID fields (slight complexity)
- Non-clustered index lookup for token queries (~0.1ms overhead)
- Naming discipline required

### Mitigations
- Strict naming: Always `External*Id`, never ambiguous
- Documentation and code comments
- Unique indexes ensure fast lookups

## Multi-IdP Migration Path

Current: Single ExternalUserId works because ZITADEL handles all social login federation.

Future: If supporting multiple IdPs (Auth0, Okta), add identity mapping table to map multiple external IDs to one internal UserId without breaking existing aggregates.
