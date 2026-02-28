# Identity Provider Migration: ZITADEL → Keycloak

**Status**: Accepted  
**Date**: 2026-02-27  
**Deciders**: oxface  
**Supersedes**: [0003-headless-onboarding.md](0003-headless-onboarding.md), [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md), [0008-authorization-hybrid-strategy.md](0008-authorization-hybrid-strategy.md), [0010-direct-tenant-id-mapping.md](0010-direct-tenant-id-mapping.md), [0014-canonical-external-identifiers.md](0014-canonical-external-identifiers.md)

## Context

ProperTea was built on ZITADEL as the identity provider, leveraging its headless onboarding API (v2 gRPC), custom Login V2 UI container, and ZITADEL-specific JWT introspection for backend services.

Several factors have changed since those decisions were made:

1. **We no longer require a custom login UI.** ADR 0003 chose headless onboarding to avoid ZITADEL's default login screen and to run a custom Next.js Login V2 container. Keycloak's recently updated hosted login UI meets our requirements without that maintenance burden. Registration still uses our own Angular page (domain data such as Tier cannot be expressed in Keycloak's UI), but the login flow is fully delegated to Keycloak.

2. **Keycloak provides first-class Organizations support** (since KC 26). This maps directly to our multi-tenancy model — one organization per tenant, with the organization ID surfaced in the token via standard Protocol Mappers. ZITADEL's organization model required three layers of configuration (Instance, Organizations, Projects, Project Grants) to achieve the same result.

3. **Simpler service account model.** ZITADEL required signed JWT credential files (one per service) to be distributed and bind-mounted into containers. Keycloak uses standard OAuth2 client credentials (client_id + secret), which integrate naturally with ASP.NET Core configuration and .NET Aspire secrets.

4. **Better .NET ecosystem support.** `Keycloak.AuthServices` (NikiforovAll) provides `Keycloak.AuthServices.Aspire.Hosting`, `AddKeycloakWebAppAuthentication`, `AddKeycloakWebApiAuthentication`, and a full Admin REST API SDK — covering every integration point we have. This is more actively maintained and better documented than the ZITADEL .NET SDK for our use cases.

5. **The product is not live.** There are no users, no production data, no migration cost. This is the right time to make this switch.

## Decision

Replace ZITADEL with Keycloak as the identity provider across all ProperTea services. Use `Keycloak.AuthServices` as the primary .NET integration library.

### Key Sub-Decisions

#### Multi-Tenancy: Keycloak Organizations Feature

Use Keycloak's native Organizations feature (KC 26+) rather than groups or separate realms. One Keycloak Organization per ProperTea tenant (property management company).

Keycloak 26+ includes an `organization` claim in the access token automatically when the `organization` scope is added to the client — no custom Protocol Mapper is required. The claim is a JSON object keyed by organization ID:

```json
{
  "organization": {
    "6ba7b810-9dad-11d1-80b4-00c04fd430c8": { "name": "Acme Holdings" }
  }
}
```

`OrganizationIdProvider` parses this claim and returns `Guid?` — `null` when the claim is absent (B2C users such as tenants/renters are not members of any Keycloak Organization and will not have this claim; this is expected). Callers enforce presence where required. The constant is renamed from `ZitadelOrgIdClaim` to `OrgIdClaim` with value `"organization"`. The org name is available from the nested object's `name` field — no separate claim mapper needed.

For the current product phase, each landlord user belongs to exactly one organization, so the claim object contains a single entry. Multi-organization membership is a Keycloak-native capability and can be enabled without protocol changes when needed.

The core principle of ADR 0010 — use the IdP's organization identifier directly as `TenantId` with no mapping layer — is preserved.

#### Token Validation: Hybrid Strategy (Introspection + Local JWT)

| Service | Strategy |
|---|---|
| Organization, User | RFC 7662 Token Introspection via `AddKeycloakWebApiAuthentication` (introspection mode) |
| Company, Property | Local JWT Bearer via standard `AddAuthentication().AddJwtBearer()` |

Organization and User services perform auth-sensitive operations (provisioning, profile management) and benefit from introspection's instant revocation guarantees. Company and Property are read-heavy and latency-sensitive; local validation is sufficient.

Keycloak's introspection endpoint is authenticated with standard OAuth2 client credentials (`client_id` + `secret`) rather than ZITADEL's signed JWT credential files.

#### Org-scoped vs User-scoped Endpoint Split

The system serves two distinct user types whose data access perspectives differ fundamentally:

- **Landlords** (B2B): access is scoped to their organization. All queries run against a single Marten tenant partition via `InvokeForTenantAsync(orgId, ...)`.
- **Tenants/renters** (B2C): access is scoped to the individual user, across any organization that may own relevant records. Queries run against a cross-tenant `IQuerySession` filtered by `userId`.

These are not variations of the same request — they use different Marten session types and different security boundaries. They are implemented as **separate Wolverine handlers and separate endpoints**:

```
GET /api/contracts        → landlord handler → InvokeForTenantAsync(orgId, GetContractsQuery)
GET /api/my/contracts     → tenant handler  → IQuerySession filtered by userId (cross-tenant)
```

The `/my/` prefix is the conventional REST idiom for "resources belonging to the authenticated user" and is well-established in SaaS APIs. No if-else inside handlers. Route groups carry different auth policies: the landlord group requires a non-null `X-Organization-Id`; the `/my/` group does not.

The Landlord BFF only exposes `/api/*` (org-scoped) routes. A future Tenant Portal BFF exposes `/api/my/*` routes. Downstream services expose both route groups; the BFF layer determines which are reachable.

#### Programmatic Organization Provisioning Retained

The `IExternalOrganizationClient` interface and the handler-level orchestration of organization registration are kept unchanged. Only the implementation changes: `ZitadelOrganizationClient` (gRPC) is replaced with `KeycloakOrganizationClient` (Keycloak Admin REST API via `Keycloak.AuthServices.Sdk`).

#### External ID Type: `string` → `Guid`

ADR 0014 mandated `string` for `OrganizationId` and `UserId` because ZITADEL used opaque non-UUID string identifiers. Keycloak uses UUIDs for all resource identifiers (users and organizations). The canonical external identifier fields are changed to `Guid`:

- `OrganizationAggregate.OrganizationId`: `string?` → `Guid?`
- `UserProfileAggregate.UserId`: `string` → `Guid`
- `UserPreferencesAggregate.ExternalUserId`: `string` → `Guid`

All other rules from ADR 0014 remain in effect: the `Guid Id` event stream key stays private; `OrganizationId` / `UserId` remain the canonical public identifiers; no `ExternalX` prefix.

Integration event contracts in `ProperTea.Contracts` are updated accordingly (`Guid` instead of `string` for these fields).

#### Registration Page: Retained; Login UI: Replaced

ADR 0003's custom registration page is partially retained. ProperTea's Organization domain carries data that Keycloak has no concept of (e.g. Tier, subscription settings). Registration must therefore remain a custom Angular page that calls the Organization Service, which in turn calls the Keycloak Admin API to create the user and organization atomically.

What changes: the custom ZITADEL Login V2 Next.js container (`zitadel-login`) is removed. Login is fully delegated to Keycloak's hosted UI. The flow becomes:

1. User visits our registration page → enters org name, email, password, plan/tier.
2. Angular calls Organization Service.
3. `KeycloakOrganizationClient` calls Keycloak Admin REST API to create the Keycloak Organization and an initial admin user.
4. Organization Service persists the `OrganizationAggregate` (with Tier and domain data) and publishes the integration event.
5. All subsequent **logins** use Keycloak's hosted login UI — no custom login page.

ADR 0003's supersession is therefore partial: the ZITADEL gRPC provisioning implementation and the custom login container are replaced; the principle of owning the registration UX is retained.

## Consequences

### Positive
- Single Keycloak container replaces two containers (`zitadel` + `zitadel-login`) and eliminates the PAT/JWT credential file management overhead.
- `Guid` type for external identifiers is semantically correct, removes `Guid.Parse()` calls from services that receive these IDs, and eliminates runtime parse errors.
- The `organization` claim is built into KC 26+ at no configuration cost — no Protocol Mapper to maintain. Org name is co-located in the same claim object.
- Keycloak's Organizations feature is actively developed and well-documented. The multi-tenancy model maps 1:1 to our requirements.
- `Keycloak.AuthServices.Aspire.Hosting` provides a purpose-built Aspire integration including realm import for local dev.

### Negative
- Keycloak is a heavier runtime than ZITADEL (JVM-based). Start time in local dev is slower.
- The Organizations feature is relatively new (KC 26). Some edge cases (e.g., users as members of multiple organizations) may have rougher edges than ZITADEL's mature implementation.
- `AddKeycloakWebApiAuthentication` with introspection requires a valid client secret in every environment. Secrets management must be set up from day one.

### Risks / Mitigation
- **Keycloak Organizations immaturity**: The feature is GA in KC 26 and the version we adopt will be ≥ 26. Mitigate by pinning the Keycloak version and reviewing release notes before upgrades.
- **`organization` claim absent for landlord users**: If the `organization` scope is not added to the `landlord-bff` client, the claim will not appear. Mitigate by asserting the claim in `realm-export.json` and in integration tests. Note: absence is *expected and correct* for B2C users (tenants/renters) — `OrganizationIdProvider` returns `null` in that case; the Landlord BFF's `OrganizationHeaderHandler` rejects the request with 401 if null.
- **Cross-tenant query safety**: Handlers on the `/my/*` path must never accept an org-scoped Marten session. Enforce this by making cross-tenant handlers take a plain `IQuerySession` (not injected via `IDocumentSession` scoped to a tenant) and always filter by `userId`.
- **Guid parsing failure on `sub` claim**: If a non-UUID `sub` is received. Mitigate with a dedicated claim validator in `AuthenticationConfig` that validates UUID format.

## Related Decisions
- [0010-direct-tenant-id-mapping.md](0010-direct-tenant-id-mapping.md): Superseded. Core principle retained; wording updated for Keycloak.
- [0014-canonical-external-identifiers.md](0014-canonical-external-identifiers.md): Superseded. Type changes from `string` to `Guid`; all other rules unchanged.
- [0003-headless-onboarding.md](0003-headless-onboarding.md): Partially superseded. ZITADEL gRPC provisioning and custom login container replaced. Registration page ownership retained.
- [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md): Superseded. Same isolation model; Keycloak Organizations replaces ZITADEL structure.
- [0008-authorization-hybrid-strategy.md](0008-authorization-hybrid-strategy.md): Superseded. OpenFGA portion unchanged; ZITADEL-specific membership handling replaced by Keycloak equivalents.

## Migration Plan
See [docs/migration/zitadel-to-keycloak.md](../migration/zitadel-to-keycloak.md) for the full file-by-file implementation plan.
