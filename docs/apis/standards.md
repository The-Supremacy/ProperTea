# API Standards

## Versioning
- Global v0 during initial development
- After go-live: per-service semantic versioning (e.g., /api/v1/property/…)
- Backward compatibility: maintain N-1 minor versions for 3 months

## Routing and Tenancy
- All tenant-aware routes include: `/api/v0/organization/{orgId}/…`
- For company-specific endpoints, prefer including companyId in the path where natural (e.g., `/api/v0/organization/{orgId}/companies/{companyId}/...`). Otherwise, the service can resolve companyId from the resource itself.
- Gateway rejects requests if orgId not in user.orgs[]
- Gateway forwards `X-Org-Id` header and, when applicable, `X-Company-Id`; services validate consistency

## Conventions
- RESTful resource naming, kebab-case paths
- Use standard HTTP verbs and status codes
- Pagination via `?page` and `?pageSize`; return `X-Total-Count`
- Filtering via query params; prefer exact names (e.g., `status=active`)
- Idempotent PUT/PATCH for updates where feasible

## Authentication
- Frontend → Gateway: Identity Service JWT (10m access, 30d refresh)
- Gateway → Services: gateway-minted internal JWT (60s, shared audience internal-services)
- Services accept only internal tokens; validate issuer, audience, expiry, signature

## RFC 7807: Problem Details for HTTP APIs
- Media type: `application/problem+json`
- Fields:
  - `type` (URI reference to human-readable documentation)
  - `title` (short, human-readable summary)
  - `status` (HTTP status code)
  - `detail` (human-readable explanation)
  - `instance` (URI reference to the specific occurrence)
- Extensions:
  - `traceId` (for correlation)
  - `organizationId`
  - `userId`
  - `companyId` (when applicable)
- Example:
