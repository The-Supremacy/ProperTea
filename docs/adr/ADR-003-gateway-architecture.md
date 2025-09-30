# ADR-003: API Gateway Architecture

## Status
Accepted

## Context

ProperTea requires a single public entry point that centralizes authentication, authorization prechecks, routing, rate limiting, and observability. The gateway must mint short-lived internal tokens for downstream services and enforce tenant access at the edge, while remaining compatible with future Azure API Management or Managed Identity adoption.

Options considered:
- Envoy: mature, YAML-driven, non-.NET stack
- Kong: feature-rich, plugin ecosystem, commercial tiers
- YARP: .NET-native, programmable in C#, integrates with ASP.NET middleware

## Decision

Use YARP (Yet Another Reverse Proxy) as the API Gateway.

Core responsibilities:
- Validate external JWTs from Identity Service (10-minute access tokens).
- Enforce organization access: reject if `organizationId` not in user.orgs[].
- Fetch permissions from Authorization Service and mint an internal JWT (60s) embedding:
  - `sub`, `orgs[]`, `roles[]`, `permissionsByService`
  - `iss=https://gateway.local`, `aud=internal-services`
- Route requests to backend services; forward `X-Org-Id` and correlation headers.
- Apply rate limiting (token bucket) keyed by route, userId, orgId|none, client IP.
- Provide observability via OpenTelemetry (traces, metrics, logs).

Key management:
- Dedicated internal issuer (separate from Identity Service).
- Monthly key rotation; keep last 2 keys active (kid header, JWKs published).
- Services validate internal tokens only (issuer/audience/signature/exp).

## Request Flow
1. Frontend → Gateway with external JWT.
2. Gateway validates external token, extracts `userId`.
3. Gateway fetches permission model for (userId, orgId).
4. Gateway verifies `organizationId` from path ∈ user.orgs[].
5. Gateway mints internal token (60s) with permissions snapshot.
6. Gateway forwards to service with internal token + `X-Org-Id`.
7. Service validates internal token and performs local permission checks.
8. Response returned through Gateway.

## Consequences

Positive:
- Strong edge enforcement of tenant context
- Short-lived internal tokens reduce security blast radius
- Deep .NET integration and programmable middleware
- Clear path to Managed Identity migration (issuer/audience swap later)

Negative:
- Gateway is a critical component and single choke point
- Additional token minting step adds complexity
- Internal token can be large with full permission snapshot

Mitigations:
- Horizontal scale out gateway; thorough health checks and alarms
- Optimize permission model size if needed (hash/version future option)
- Robust observability on auth, routing, and latency

## Operational Notes
- Health endpoints: `/health/live`, `/health/ready`, `/metrics`
- Problem+JSON (RFC 7807) for errors with extensions: `traceId`, `organizationId`, `userId`
- Local HTTPS with Traefik/mkcert for prod parity
- Rate limiting responses include `Retry-After`

## Alternatives and Migration
- Can later adopt Azure API Management for edge concerns; keep YARP as internal gateway if desired.
- For Managed Identity-based service auth, swap to AAD issuer and update audiences; retain org enforcement and permission fetch logic at the edge.
