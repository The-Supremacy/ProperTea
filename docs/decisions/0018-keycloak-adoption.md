# Identity Provider Migration: ZITADEL → Keycloak

**Status**: Accepted  
**Date**: 2026-02-27  
**Deciders**: oxface  
**Supersedes**: [0003-headless-onboarding.md](0003-headless-onboarding.md), [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md), [0008-authorization-hybrid-strategy.md](0008-authorization-hybrid-strategy.md), [0010-direct-tenant-id-mapping.md](0010-direct-tenant-id-mapping.md), [0014-canonical-external-identifiers.md](0014-canonical-external-identifiers.md)

## Context

ProperTea was built on ZITADEL as the identity provider, leveraging its headless onboarding API (v2 gRPC), custom Login V2 UI container, and ZITADEL-specific JWT introspection for backend services.

Several factors have changed since those decisions were made:

1. **We no longer require a headless/custom registration flow.** ADR 0003 chose headless onboarding specifically to avoid ZITADEL's default UI. Keycloak recently released a significantly improved hosted login and account UI that meets our branding requirements without maintaining a custom Next.js login container.

2. **Keycloak provides first-class Organizations support** (since KC 26). This maps directly to our multi-tenancy model — one organization per tenant, with the organization ID surfaced in the token via standard Protocol Mappers. ZITADEL's organization model required three layers of configuration (Instance, Organizations, Projects, Project Grants) to achieve the same result.

3. **Simpler service account model.** ZITADEL required signed JWT credential files (one per service) to be distributed and bind-mounted into containers. Keycloak uses standard OAuth2 client credentials (client_id + secret), which integrate naturally with ASP.NET Core configuration and .NET Aspire secrets.

4. **Better .NET ecosystem support.** `Keycloak.AuthServices` (NikiforovAll) provides `Keycloak.AuthServices.Aspire.Hosting`, `AddKeycloakWebAppAuthentication`, `AddKeycloakWebApiAuthentication`, and a full Admin REST API SDK — covering every integration point we have. This is more actively maintained and better documented than the ZITADEL .NET SDK for our use cases.

5. **The product is not live.** There are no users, no production data, no migration cost. This is the right time to make this switch.

## Decision

Replace ZITADEL with Keycloak as the identity provider across all ProperTea services. Use `Keycloak.AuthServices` as the primary .NET integration library.

### Key Sub-Decisions

#### Multi-Tenancy: Keycloak Organizations Feature

Use Keycloak's native Organizations feature (KC 26+) rather than groups or separate realms. One Keycloak Organization per ProperTea tenant (property management company). The organization ID is surfaced in the access token via a custom Organization Attribute Protocol Mapper as a `tenant_id` claim.

This replaces ZITADEL's `urn:zitadel:iam:user:resourceowner:id` claim. The claim has been renamed to the IdP-agnostic `tenant_id` in `OrganizationIdProvider`, removing the last identifier coupling to ZITADEL from shared infrastructure.

The core principle of ADR 0010 — use the IdP's organization identifier directly as `TenantId` with no mapping layer — is preserved.

#### Token Validation: Hybrid Strategy (Introspection + Local JWT)

| Service | Strategy |
|---|---|
| Organization, User | RFC 7662 Token Introspection via `AddKeycloakWebApiAuthentication` (introspection mode) |
| Company, Property | Local JWT Bearer via standard `AddAuthentication().AddJwtBearer()` |

Organization and User services perform auth-sensitive operations (provisioning, profile management) and benefit from introspection's instant revocation guarantees. Company and Property are read-heavy and latency-sensitive; local validation is sufficient.

Keycloak's introspection endpoint is authenticated with standard OAuth2 client credentials (`client_id` + `secret`) rather than ZITADEL's signed JWT credential files.

#### Programmatic Organization Provisioning Retained

The `IExternalOrganizationClient` interface and the handler-level orchestration of organization registration are kept unchanged. Only the implementation changes: `ZitadelOrganizationClient` (gRPC) is replaced with `KeycloakOrganizationClient` (Keycloak Admin REST API via `Keycloak.AuthServices.Sdk`).

#### External ID Type: `string` → `Guid`

ADR 0014 mandated `string` for `OrganizationId` and `UserId` because ZITADEL used opaque non-UUID string identifiers. Keycloak uses UUIDs for all resource identifiers (users and organizations). The canonical external identifier fields are changed to `Guid`:

- `OrganizationAggregate.OrganizationId`: `string?` → `Guid?`
- `UserProfileAggregate.UserId`: `string` → `Guid`
- `UserPreferencesAggregate.ExternalUserId`: `string` → `Guid`

All other rules from ADR 0014 remain in effect: the `Guid Id` event stream key stays private; `OrganizationId` / `UserId` remain the canonical public identifiers; no `ExternalX` prefix.

Integration event contracts in `ProperTea.Contracts` are updated accordingly (`Guid` instead of `string` for these fields).

#### Headless Onboarding: Removed

ADR 0003's headless onboarding flow is superseded in full. Registration now goes through Keycloak's hosted UI. The custom ZITADEL Login V2 container (`zitadel-login`) is removed with no replacement — Keycloak provides its own.

The `Organization Service` retains its local aggregate provisioning logic (Marten, integration events). The difference is that the initial user+org creation happens in Keycloak's UI rather than via a custom Angular registration page calling the Organization Service.

> If a custom registration flow is needed in future, it can be built as a Keycloak extension (Action Token or User Registration SPI) rather than a custom auth UI.

## Consequences

### Positive
- Single Keycloak container replaces two containers (`zitadel` + `zitadel-login`) and eliminates the PAT/JWT credential file management overhead.
- `Guid` type for external identifiers is semantically correct, removes `Guid.Parse()` calls from services that receive these IDs, and eliminates runtime parse errors.
- `tenant_id` claim name is readable and IdP-agnostic; future IdP migrations only require a Protocol Mapper change, not shared library updates.
- Keycloak's Organizations feature is actively developed and well-documented. The multi-tenancy model maps 1:1 to our requirements.
- `Keycloak.AuthServices.Aspire.Hosting` provides a purpose-built Aspire integration including realm import for local dev.

### Negative
- Keycloak is a heavier runtime than ZITADEL (JVM-based). Start time in local dev is slower.
- The Organizations feature is relatively new (KC 26). Some edge cases (e.g., users as members of multiple organizations) may have rougher edges than ZITADEL's mature implementation.
- `AddKeycloakWebApiAuthentication` with introspection requires a valid client secret in every environment. Secrets management must be set up from day one.

### Risks / Mitigation
- **Keycloak Organizations immaturity**: The feature is GA in KC 26 and the version we adopt will be ≥ 26. Mitigate by pinning the Keycloak version and reviewing release notes before upgrades.
- **`tenant_id` claim not populated**: If the Protocol Mapper is misconfigured, all requests fail at the `OrganizationIdProvider`. Mitigate with a startup health check that validates token claims during local dev smoke tests.
- **Guid parsing failure on `sub` claim**: If a non-UUID `sub` is received. Mitigate with a dedicated claim validator in `AuthenticationConfig` that validates UUID format.

## Related Decisions
- [0010-direct-tenant-id-mapping.md](0010-direct-tenant-id-mapping.md): Superseded. Core principle retained; wording updated for Keycloak.
- [0014-canonical-external-identifiers.md](0014-canonical-external-identifiers.md): Superseded. Type changes from `string` to `Guid`; all other rules unchanged.
- [0003-headless-onboarding.md](0003-headless-onboarding.md): Superseded. Replaced by Keycloak hosted UI.
- [0007-organization-multi-tenancy.md](0007-organization-multi-tenancy.md): Superseded. Same isolation model; Keycloak Organizations replaces ZITADEL structure.
- [0008-authorization-hybrid-strategy.md](0008-authorization-hybrid-strategy.md): Superseded. OpenFGA portion unchanged; ZITADEL-specific membership handling replaced by Keycloak equivalents.

## Migration Plan
See [docs/migration/zitadel-to-keycloak.md](../migration/zitadel-to-keycloak.md) for the full file-by-file implementation plan.
