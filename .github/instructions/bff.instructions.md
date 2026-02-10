---
applyTo: "**/Bff/**"
---

# BFF Development (Landlord BFF)

The BFF is a **pass-through gateway**. It authenticates requests and forwards them to backend services.

## Strict Rules
- **No business logic**. No validation beyond auth. No data transformation beyond DTO mapping.
- **No direct database access**. All data comes from backend service HTTP calls.
- If you find yourself writing complex logic, it belongs in a backend service.

## Patterns
- **Typed HttpClients**: One per backend service (e.g., `CompanyClient`). Configured with `AddUserAccessTokenHandler()` and `OrganizationHeaderHandler`.
- **Endpoint groups**: `MapGroup("/api/{resource}")` with `.RequireAuthorization()`.
- **Route handlers**: Static methods that delegate to typed clients.
- **Anonymous clients**: Separate client class for unauthenticated flows (e.g., `OrganizationClientAnonymous`).

## Auth Flow
- OIDC Code Flow with ZITADEL. Sessions stored in Redis.
- `OrganizationHeaderHandler` extracts ZITADEL org claim from token and injects `X-Organization-Id` header.
- `/api/session` endpoint returns user context (read from JWT claims, no service calls).

## Style
- Use `_ =` discard for `MapGet`/`MapPost` return values.
- Group endpoints by resource in separate static classes.
- Add `.WithTags()` and `.WithName()` for OpenAPI.
