# Multi-Tenancy Flow

How tenant isolation works end-to-end in ProperTea.

## Request Flow

```
Browser → BFF → Service → Marten (PostgreSQL)
```

### 1. Authentication (BFF)
User logs in via OIDC Code Flow with ZITADEL. Session stored in Redis.
JWT contains `urn:zitadel:iam:org:id` claim with the ZITADEL organization ID.

### 2. Header Injection (BFF)
`OrganizationHeaderHandler` (a `DelegatingHandler` on typed HttpClients) extracts the org claim and adds `X-Organization-Id` header to all downstream requests.

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
- ZITADEL org ID is used directly as Marten `TenantId` (ADR 0010). No internal mapping.
- Aggregates implement `ITenanted` for Marten to stamp the tenant automatically.
- Cross-tenant access (contractors viewing work orders) uses OpenFGA contextual tuples (ADR 0004), not Marten tenant switching.
