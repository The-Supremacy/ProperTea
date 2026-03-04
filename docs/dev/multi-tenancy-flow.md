# Multi-Tenancy Flow

How tenant isolation works end-to-end in ProperTea.

## Request Flow

```
Browser → BFF → Service → Marten (PostgreSQL)
```

### 1. Authentication (BFF)
User logs in via OIDC Code Flow with Keycloak. Session stored in Redis.
JWT contains an `organization` claim object with the Keycloak organization ID as the key.

### 2. Header Injection (BFF)
`OrganizationHeaderHandler` (a `DelegatingHandler` on typed HttpClients) extracts the org ID from the `organization` claim and adds `X-Organization-Id` header to all downstream requests.

### 3. Tenant Extraction (Service)
`IOrganizationIdProvider.GetOrganizationId()` reads `X-Organization-Id` from the incoming request headers.

### 4. Command Dispatch (Service Endpoint)
Endpoints dispatch commands with tenant scoping:
```csharp
var tenantId = orgProvider.GetOrganizationId();
var result = await bus.InvokeForTenantAsync<TResult>(tenantId, command);
```

### 5. Data Isolation (Marten)
Marten's conjoined tenancy (`AllDocumentsAreMultiTenanted`) automatically scopes all queries and event streams to the tenant. No manual filtering needed.

## Key Design Decisions
- Keycloak org ID is used directly as Marten `TenantId`. No internal mapping.
- Aggregates implement `ITenanted` for Marten to stamp the tenant automatically.
- Cross-tenant access (contractors viewing work orders) will use OpenFGA contextual tuples (planned), not Marten tenant switching.
