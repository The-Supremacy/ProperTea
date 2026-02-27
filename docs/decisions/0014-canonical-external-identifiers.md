# Canonical External Identifiers for Users and Organizations

**Status**: Superseded by [0018-keycloak-adoption.md](0018-keycloak-adoption.md)
**Date**: 2026-02-14
**Deciders**: oxface, GitHub Copilot
**Supersedes**: [0009-user-identity-strategy.md](0009-user-identity-strategy.md)

## Context

ADR 0009 established a "Keep Both IDs" pattern: each aggregate has an internal `Guid Id` (Marten stream key) and an external `string External*Id` (ZITADEL identifier). The intent was to decouple domain identity from authentication identity and preserve a multi-IdP migration path.

In practice, this created persistent confusion:

1. **Which ID to use?** Every developer touching Organization or User references must decide between internal and external ID. The naming convention (`ExternalOrganizationId` vs `OrganizationId`) helps, but the decision recurs at every integration point.

2. **Semantic mismatch in integration events.** Company and Property services populate `Guid OrganizationId` in their integration events via `Guid.Parse(session.TenantId)`. This value is actually the ZITADEL org ID (which ADR 0010 established as the Marten TenantId), not the Organization aggregate's internal `Guid Id`. The field name suggests one thing; the value is another.

3. **ADR 0009 contradicts ADR 0010.** ADR 0009 says integration events should use internal Guid IDs. ADR 0010 says the ZITADEL org ID is the tenant discriminator with no mapping layer. The result: services that don't own the Organization aggregate cannot obtain its internal Guid without an extra cross-service call, so they use the tenant ID instead — but cast it to Guid to satisfy the contract type.

4. **The internal Guid adds no value outside its owning service.** No cross-service relationship, foreign key, or API endpoint actually requires the Organization or User aggregate's internal `Guid Id`. Every access path starts from a token claim (ZITADEL ID), and Marten's tenant scoping uses the ZITADEL ID directly.

5. **The multi-IdP abstraction is premature.** Switching identity providers requires migrating tokens, claims, user accounts, and org structures regardless. An internal ID layer saves one table migration but costs daily cognitive overhead.

## Decision

**ZITADEL identifiers are the canonical external identities for Organizations and Users.** The Marten `Guid Id` (event stream key) is a private implementation detail that never appears in APIs, integration events, or cross-service contracts.

### Rules

| Concern | Rule |
|---|---|
| **Aggregate internals** | `Guid Id` remains as the Marten event stream key. It is never exposed outside the owning service. |
| **Canonical identity** | `string OrganizationId` (ZITADEL org ID) and `string UserId` (ZITADEL `sub` claim) are the identifiers used in all public contracts. |
| **Integration events** | All `OrganizationId` and `UserId` fields are `string`, carrying ZITADEL identifiers. No `Guid` casting. |
| **REST APIs** | Endpoints that accept or return Organization/User IDs use `string`. No `/organizations/{guid}` or `/users/{guid}` routes. |
| **Multi-tenancy** | Unchanged. `session.TenantId` is already the ZITADEL org ID (ADR 0010). Use it directly as `string OrganizationId` in events — no `Guid.Parse()`. |
| **Naming** | Drop the `External` prefix. The fields are `OrganizationId` and `UserId`, not `ExternalOrganizationId` / `ExternalUserId`. The Marten stream key is `Id` (private, never in contracts). |
| **Marten stream identity** | Stays `StreamIdentity.AsGuid`. No configuration change needed. The Guid is just an internal storage detail. |

### Organization Identity

```
Registration flow (unchanged):
1. Call ZITADEL → returns string organizationId
2. Use organizationId as canonical identity
3. Start Marten stream with Guid.NewGuid() (private stream key)
4. Store organizationId on aggregate as OrganizationId
5. Publish integration event with string OrganizationId
```

The `OrganizationAggregate` retains `Guid Id` for Marten plumbing and gains `string OrganizationId` (renamed from `ExternalOrganizationId`). The `Guid Id` is never included in integration events or API responses.

### User Identity

```
JIT creation flow (unchanged):
1. Read sub claim from JWT → string userId
2. Use userId as canonical identity
3. Start Marten stream with Guid.NewGuid() (private stream key)
4. Store userId on aggregate as UserId
5. Publish integration event with string UserId
```

The `UserProfileAggregate` retains `Guid Id` for Marten plumbing and gains `string UserId` (renamed from `ExternalUserId`). The `Guid Id` is never included in integration events or API responses.

### Other Entities (Company, Property, Unit)

No change. These entities have no ZITADEL counterpart. Their `Guid Id` is their canonical identity. Their `OrganizationId` fields in integration events change from `Guid` to `string` (now directly `session.TenantId`, no parsing).

## Consequences

### Positive

- **Single identity per entity.** No more "which ID do I use?" decisions. ZITADEL ID for Organization/User, Guid for everything else.
- **Eliminates `Guid.Parse(session.TenantId)`.** Nine call sites across Company and Property services simplified to direct string assignment.
- **Consistent with ADR 0010.** The tenant ID is a string everywhere — in Marten, in events, in APIs.
- **Fixes semantic mismatch.** Integration event `OrganizationId` fields now honestly represent what they contain (ZITADEL org ID), with a matching `string` type.
- **Simpler contracts.** `IOrganizationRegistered` drops the `ExternalOrganizationId` property — there is only `OrganizationId`.

### Negative

- **Breaking change to integration event contracts.** `OrganizationId` changes from `Guid` to `string` across all events. Requires versioned rollout (v2 events).
- **ZITADEL coupling is now explicit.** The internal aggregate identity is tied to an external system. Previously this was implicit (via ADR 0010); now it is a stated design choice.
- **Migration script required.** Existing event store data references `ExternalOrganizationId` / `ExternalUserId` field names. Projections need rebuilding.

### Risks / Mitigation

- **IdP migration** → One-time data migration script to remap identifiers. This was already the case under ADR 0010; making it explicit doesn't increase the risk. Any IdP switch requires token, claim, and account migration regardless.
- **ZITADEL ID format changes** → Validate format at authentication boundary. ZITADEL uses stable numeric string IDs. If format changes, it's a ZITADEL breaking change affecting all customers, not just us.
- **Integration event version migration** → Use Wolverine's message identity versioning. Publish v2 events alongside v1 during transition. Subscribers migrate individually.

## Alternatives Considered

1. **Keep Both IDs (ADR 0009 status quo)**: Rejected. The confusion cost is ongoing and compounds with every new service and developer. The multi-IdP benefit is speculative.
2. **Switch Marten to string stream identity**: Rejected. Requires per-service `StreamIdentity` configuration changes and affects all aggregates in a service, not just Organization/User. Marten is optimized for Guid streams. The Guid stream key works fine as a private detail.
3. **Store internal Guid in ZITADEL metadata**: Rejected. Adds ZITADEL API calls on registration, introduces synchronization concerns, and still requires deciding which ID to use downstream.
4. **Remint tokens with internal IDs**: Rejected. Complex, security-sensitive, and defeats the purpose of using an external IdP.

## Related Decisions

- [0009-user-identity-strategy.md](0009-user-identity-strategy.md): Superseded by this decision
- [0010-direct-tenant-id-mapping.md](0010-direct-tenant-id-mapping.md): Complementary — established ZITADEL org ID as TenantId. This ADR extends the same principle to all cross-service identity references.
- [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md): Unchanged — ZITADEL organizations remain the isolation boundary
