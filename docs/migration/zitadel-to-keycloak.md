# Migration: ZITADEL → Keycloak

**Status**: Planned  
**Date**: 2026-02-27  
**Related ADR**: [0018-keycloak-adoption.md](../decisions/0018-keycloak-adoption.md)

## Reason for Change

See ADR 0018 for the full rationale. In short:

- We no longer require a headless/custom registration UI. Keycloak's recently updated hosted UI meets our needs without the maintenance burden of running a custom OIDC login layer.
- Keycloak's Organizations feature (available since KC 26) provides first-class multi-tenancy support that maps directly to our model.
- Simpler service account model: Keycloak uses standard OAuth2 client credentials; ZITADEL required proprietary signed JWT files.
- The product is not live. No data migration is needed — databases will be dropped and re-seeded.

---

## Overview of Changes

| Area | Change |
|---|---|
| AppHost containers | Replace `zitadel` + `zitadel-login` containers with a single Keycloak container |
| NuGet packages | Remove `Zitadel` (all services); add `Keycloak.AuthServices.*` |
| BFF authentication | `AddZitadel()` → `AddKeycloakWebAppAuthentication()` |
| Backend token validation | `AddZitadelIntrospection()` → `AddKeycloakWebApiAuthentication()` with introspection (Organization, User services); Company and Property already use standard JWT Bearer — authority URL change only |
| Organization client | Rewrite `ZitadelOrganizationClient` against Keycloak Admin REST API; rename to `KeycloakOrganizationClient` |
| Tenant ID claim | `urn:zitadel:iam:user:resourceowner:id` → `tenant_id` (custom Protocol Mapper) |
| External ID types | `string OrganizationId`, `string UserId`, `string ExternalUserId` → `Guid` |
| Angular SPA | No changes required |
| Config files | Remove `Config/zitadel/`; add `Config/keycloak/realm-export.json` |
| Documentation | Update architecture.md, multi-tenancy-flow.md, affected ADRs |

---

## Keycloak Architecture

### Realm Structure

```
Keycloak Instance (local: http://localhost:9080)
└─ Realm: propertea
   ├─ Clients
   │  ├─ landlord-bff        (confidential, Authorization Code + PKCE)
   │  ├─ organization-svc    (confidential, client credentials — introspection)
   │  └─ user-svc            (confidential, client credentials — introspection)
   ├─ Organizations (KC 26+ feature)
   │  ├─ "Acme Holdings"  → id: <uuid>
   │  └─ "Widgets Realty" → id: <uuid>
   └─ Protocol Mappers (on landlord-bff client)
      ├─ tenant_id    — Organization ID → token claim
      └─ org_name     — Organization name → token claim
```

### Multi-Tenancy: How TenantId Flows

Keycloak's Organizations feature assigns each user to one organization. When a user authenticates through `landlord-bff`, the token contains:

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "tenant_id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "org_name": "Acme Holdings",
  "email": "john@acme.com"
}
```

`tenant_id` is added by a custom **Organization Attribute Protocol Mapper** that reads the user's active organization ID. The claim name `tenant_id` is IdP-agnostic — `OrganizationIdProvider` becomes unaware of Keycloak specifics.

The full flow is unchanged from the existing architecture:
1. BFF validates the session cookie; `OrganizationHeaderHandler` reads `tenant_id` → writes `X-Organization-Id` header.
2. Backend services read `X-Organization-Id`; `OrganizationIdProvider.GetOrganizationId()` resolves it.
3. Marten tenant scope is set: `bus.InvokeForTenantAsync(tenantId, command)`.

### Token Validation Split

| Service | Strategy | Reason |
|---|---|---|
| Organization Service | Introspection (RFC 7662) | Auth-sensitive; provisioning operations require verified active tokens |
| User Service | Introspection (RFC 7662) | Auth-sensitive; profile operations require verified active tokens |
| Company Service | Local JWT Bearer | Performance-sensitive; standard audience validation sufficient |
| Property Service | Local JWT Bearer | Performance-sensitive; standard audience validation sufficient |

---

## File-by-File Changes

### AppHost

**`orchestration/ProperTea.AppHost/ProperTea.AppHost.csproj`**
- Remove: `Zitadel` NuGet (if any direct reference)
- Add: `Keycloak.AuthServices.Aspire.Hosting`

**`orchestration/ProperTea.AppHost/AppHost.cs`**
- Remove: `zitadel` container registration (`ghcr.io/zitadel/zitadel:v4.10.0`)
- Remove: `zitadel-login` container registration (`ghcr.io/zitadel/zitadel-login:v4.10.0`)
- Remove: all `Zitadel__ServiceAccountJwtPath` and `Zitadel__AppJwtPath` env var injections
- Add: Keycloak resource via `AddKeycloak(...)` from Aspire hosting; bind-mount `Config/keycloak/` for realm import
- Update: `OIDC__Authority` on all services → `http://keycloak:8080/realms/propertea`
- Add: `Keycloak__AuthServerUrl`, `Keycloak__Realm`, `Keycloak__Resource`, `Keycloak__Credentials__Secret` for Organization and User services (introspection requires client credentials)
- Add: equivalent Keycloak config vars for BFF (OIDC client)

**`orchestration/ProperTea.AppHost/Config/`**
- Remove: `Config/zitadel/` directory and all contents (`organization-service.json`, `organization-app.json`, `user-service.json`, `user-app.json`, `login-ui-client.pat`)
- Add: `Config/keycloak/realm-export.json` — realm definition for local dev (realm name, clients with redirect URIs, Organization feature enabled, Protocol Mappers for `tenant_id` and `org_name`)

---

### Shared — ProperTea.Infrastructure.Common

**`shared/ProperTea.Infrastructure.Common/Auth/OrganizationIdProvider.cs`**
- Rename constant: `ZitadelOrgIdClaim` → `TenantIdClaim`
- Update value: `"urn:zitadel:iam:user:resourceowner:id"` → `"tenant_id"`

**`shared/ProperTea.Infrastructure.Common/OpenApi/OAuth2SecuritySchemeTransformer.cs`**
- Replace `urn:zitadel:iam:*` scope names with `openid profile email organization`

---

### BFF

**`apps/portals/landlord/bff/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj`**
- Remove: `<PackageReference Include="Zitadel" />`
- Add: `<PackageReference Include="Keycloak.AuthServices.Authentication" />`

**`apps/portals/landlord/bff/ProperTea.Landlord.Bff/Config/AuthenticationConfig.cs`**
- Replace `AddZitadel(...)` call with `AddKeycloakWebAppAuthentication(builder.Configuration)`
- Replace scopes: remove `urn:zitadel:iam:user:resourceowner`, `urn:zitadel:iam:org:project:roles`, `urn:zitadel:iam:org:project:id:zitadel:aud`; add `openid profile email organization`

**`apps/portals/landlord/bff/ProperTea.Landlord.Bff/Config/OpenApiConfig.cs`**
- Replace `urn:zitadel:iam:*` selected scope strings

**`apps/portals/landlord/bff/ProperTea.Landlord.Bff/Auth/OrganizationHeaderHandler.cs`**
- Update reference: `OrganizationIdProvider.ZitadelOrgIdClaim` → `OrganizationIdProvider.TenantIdClaim`

**`apps/portals/landlord/bff/ProperTea.Landlord.Bff/Session/SessionEndpoints.cs`**
- Replace hardcoded `"urn:zitadel:iam:user:resourceowner:id"` → use `OrganizationIdProvider.TenantIdClaim`
- Replace hardcoded `"urn:zitadel:iam:user:resourceowner:name"` → use `"org_name"` (custom mapped claim)

---

### Organization Service

**`apps/services/ProperTea.Organization/ProperTea.Organization.csproj`**
- Remove: `<PackageReference Include="Zitadel" />`
- Add: `<PackageReference Include="Keycloak.AuthServices.Authentication" />`
- Add: `<PackageReference Include="Keycloak.AuthServices.Sdk" />`

**`apps/services/ProperTea.Organization/Config/AuthenticationConfig.cs`**
- Replace `AddZitadelIntrospection()` + `Application.LoadFromJsonFile(appJwtPath)` credential setup
- Add `AddKeycloakWebApiAuthentication(configuration)` configured with introspection: set `KeycloakAuthenticationOptions.UseIntrospection = true`
- Remove `Zitadel:AppJwtPath` configuration key; authentication credentials now come from `Keycloak:Credentials:Secret`

**`apps/services/ProperTea.Organization/Config/OpenApiConfig.cs`**
- Replace `urn:zitadel:iam:*` scope strings

**`apps/services/ProperTea.Organization/Features/Organizations/Configuration/OrganizationMartenConfiguration.cs`**
- Remove: `ServiceAccount.LoadFromJsonFile(...)` registration
- Remove: `ZitadelOrganizationClient` DI registration
- Remove: `Zitadel:ServiceAccountJwtPath` configuration key
- Add: `services.AddKeycloakAdminHttpClient(configuration)` for Keycloak Admin REST API
- Add: `services.AddScoped<IExternalOrganizationClient, KeycloakOrganizationClient>()`

**`apps/services/ProperTea.Organization/Infrastructure/ZitadelOrganizationClient.cs`**
- Rename file: → `KeycloakOrganizationClient.cs`
- Rename class: `ZitadelOrganizationClient` → `KeycloakOrganizationClient`
- Replace gRPC implementation (`Zitadel.Api.Clients.OrganizationService`, `Zitadel.Org.V2.*`) with Keycloak Admin REST API calls via `IKeycloakRealmClient` from `Keycloak.AuthServices.Sdk`
- `AddOrganizationAsync` → `POST /admin/realms/propertea/organizations`
- `GetOrganizationAsync` → `GET /admin/realms/propertea/organizations/{id}`
- `ListOrganizationsAsync` → `GET /admin/realms/propertea/organizations`

**`apps/services/ProperTea.Organization/Infrastructure/IExternalOrganizationClient.cs`**
- No changes. The interface contract is IdP-agnostic.

**`apps/services/ProperTea.Organization/Features/Organizations/OrganizationAggregate.cs`**
- Change `OrganizationId`: `string?` → `Guid?`
- Update `OrganizationLinked` event: carry `Guid OrganizationId`
- Update `Apply(OrganizationLinked)` to parse from Keycloak's UUID response
- Update Marten unique index configuration if the type change affects indexing

---

### User Service

**`apps/services/ProperTea.User/ProperTea.User.csproj`**
- Remove: `<PackageReference Include="Zitadel" />`
- Add: `<PackageReference Include="Keycloak.AuthServices.Authentication" />`

**`apps/services/ProperTea.User/Config/AuthenticationConfig.cs`**
- Same change as Organization service: replace `AddZitadelIntrospection()` with `AddKeycloakWebApiAuthentication()` + introspection

**`apps/services/ProperTea.User/Config/OpenApiConfig.cs`**
- Replace `urn:zitadel:iam:*` scope strings

**`apps/services/ProperTea.User/Features/UserProfiles/UserProfileAggregate.cs`**
- Change `UserId`: `string` → `Guid`
- Update all handlers creating or querying by `UserId` to use `Guid.Parse(user.FindFirstValue("sub")!)`

**`apps/services/ProperTea.User/Features/UserPreferences/UserPreferencesAggregate.cs`**
- Change `ExternalUserId`: `string` → `Guid`
- Update all handlers creating or querying by `ExternalUserId`

---

### Company and Property Services

**`apps/services/ProperTea.Company/Config/OpenApiConfiguration.cs`**  
**`apps/services/ProperTea.Property/Configuration/OpenApiConfiguration.cs`**
- Replace `urn:zitadel:iam:*` scope strings
- No auth code changes; these services already use standard `AddAuthentication().AddJwtBearer()`. The authority URL change is injected by AppHost.

---

### Angular SPA

No changes required. The SPA uses the BFF session pattern exclusively — it has no OIDC library and no ZITADEL references. The login redirect goes to `/auth/login` on the BFF, which triggers the OIDC challenge to Keycloak instead of ZITADEL.

---

## External ID Type Change: `string` → `Guid`

Keycloak uses UUIDs for all resource identifiers (users, organizations). This makes `Guid` the correct type for the canonical external identifiers previously kept as `string` to accommodate ZITADEL's opaque ID format.

This partially supersedes the type rule in ADR 0014, which mandated `string` to avoid `Guid.Parse()` on non-UUID ZITADEL IDs. With Keycloak, the IDs are definitively UUIDs.

**Parsing from JWT:**
```csharp
// User aggregates
Guid userId = Guid.Parse(httpContext.User.FindFirstValue("sub")!);

// Organization aggregate (from Keycloak Admin API response)
Guid organizationId = Guid.Parse(keycloakOrganizationResponse.Id);
```

**Impact on contracts:** All integration events and API contracts that currently carry `string OrganizationId` / `string UserId` must be updated to `Guid`. Update [shared/ProperTea.Contracts/](../../shared/ProperTea.Contracts/) accordingly. See the contracts update tracking below.

**Contracts to update:**
- All events in `shared/ProperTea.Contracts/Events/` that carry `OrganizationId` or `UserId` fields

---

## Configuration Reference

### Keycloak section in `appsettings.json` (Organization / User services — introspection)
```json
{
  "Keycloak": {
    "realm": "propertea",
    "auth-server-url": "http://localhost:9080/",
    "ssl-required": "none",
    "resource": "organization-svc",
    "verify-token-audience": true,
    "credentials": {
      "secret": "change-me-in-secrets"
    },
    "confidential-port": 0,
    "use-resource-role-mappings": true
  }
}
```

### Keycloak section in `appsettings.json` (BFF — Authorization Code Flow)
```json
{
  "Keycloak": {
    "realm": "propertea",
    "auth-server-url": "http://localhost:9080/",
    "ssl-required": "none",
    "resource": "landlord-bff",
    "credentials": {
      "secret": "change-me-in-secrets"
    }
  }
}
```

### Company / Property services — authority URL only
```json
{
  "Authentication": {
    "Authority": "http://localhost:9080/realms/propertea",
    "Audience": "account"
  }
}
```

---

## Documentation Updates

| File | Change |
|---|---|
| [docs/architecture.md](../architecture.md) | Replace ZITADEL with Keycloak in tech stack table and service diagram |
| [docs/dev/multi-tenancy-flow.md](../dev/multi-tenancy-flow.md) | Update claim names (`urn:zitadel:*` → `tenant_id`), update IdP references |
| [docs/decisions/0003-headless-onboarding.md](../decisions/0003-headless-onboarding.md) | Status: Superseded by 0018 |
| [docs/decisions/0007-organization-multi-tenancy.md](../decisions/0007-organization-multi-tenancy.md) | Status: Superseded by 0018; rewrite IdP structure section |
| [docs/decisions/0008-authorization-hybrid-strategy.md](../decisions/0008-authorization-hybrid-strategy.md) | Status: Superseded by 0018; rewrite "ZITADEL Handles" section |
| [docs/decisions/0010-direct-tenant-id-mapping.md](../decisions/0010-direct-tenant-id-mapping.md) | Status: Superseded by 0018; core decision unchanged, IdP wording updated |
| [docs/decisions/0014-canonical-external-identifiers.md](../decisions/0014-canonical-external-identifiers.md) | Status: Superseded by 0018 (type change: string → Guid; all other rules remain) |
| [README.md](../../README.md) | Update tech stack list |

---

## Verification Checklist

- [ ] `dotnet build ProperTea.slnx` — zero warnings/errors (`TreatWarningsAsErrors=true`)
- [ ] `dotnet run --project orchestration/ProperTea.AppHost` — Aspire dashboard shows Keycloak healthy, all services up
- [ ] Navigate to `http://localhost:4200` — redirect lands on Keycloak's hosted login UI
- [ ] Log in; verify `/api/session` returns correct `organizationName` and `tenantId` from Keycloak-issued token
- [ ] Perform an action that invokes Organization service — confirm Keycloak org ID propagates as `X-Organization-Id` and Marten tenant scope resolves correctly
- [ ] Register a new organization — confirm `KeycloakOrganizationClient.AddOrganizationAsync` calls Keycloak Admin REST API and returns a valid UUID
- [ ] Confirm `OrganizationId`, `UserId`, `ExternalUserId` fields store as `Guid` in Marten
- [ ] Run all existing integration tests

---

## Out of Scope

- Data migration: not required (product not live; databases will be dropped)
- Keycloak production deployment / Helm chart: tracked separately under `deploy/`
- OpenFGA integration: not affected by this change
- RabbitMQ / Wolverine messaging: not affected
