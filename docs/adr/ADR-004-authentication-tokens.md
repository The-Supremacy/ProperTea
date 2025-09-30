# ADR-004: Authentication Token Strategy

## Status
Accepted

## Context
We need a secure, performant, and evolvable authentication strategy. External clients authenticate via the Identity Service, while internal services should not directly trust external tokens. We also want a clear migration path toward Azure Managed Identities later.

Key requirements:
- Frontend receives access/refresh tokens from Identity Service
- Gateway validates external tokens and enforces organization access
- Backend services only trust short-lived internal tokens
- Minimal coupling; easy rotation of keys and issuers later
- Permissions may apply to the entire organization or be scoped to specific companies within the organization

## Decision

- External authentication: Identity Service issues access tokens (10 minutes) and refresh tokens (30 days).
- Gateway validates external token, fetches permissions, and mints a short-lived internal JWT (60 seconds) embedding:
  - userId (sub), orgs[], roles[], permissionsByService
  - Optional company scoping via `permissionScopes` mapping (see below)
  - iss=https://gateway.local, aud=internal-services
- Backend services validate and accept only gateway-minted internal tokens.
- Gateway publishes JWKs for internal tokens; monthly key rotation with the last 2 keys active.
- Failure mode: fail closed when Authorization Service is unavailable for permission fetching.

## Consequences

Positive:
- Short-lived internal tokens reduce blast radius
- Clear trust boundary: services trust the gateway issuer only
- Easy migration to Managed Identity: replace issuer/audience and validation config
- Caching of permissions inside token improves performance

Negative:
- Gateway becomes authentication broker and a critical path
- Larger internal tokens due to permissions snapshot
- Permission changes take up to 60 seconds to reflect

Mitigations:
- Horizontal scale gateway and add robust health and metrics
- Consider later optimization with version/hash in token if size grows
- Maintain excellent observability and alarms on gateway failures
