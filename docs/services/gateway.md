# API Gateway Service

## Purpose
Single public entry point providing authentication, authorization prechecks, routing, rate limiting, and observability.

## Responsibilities
- Validate external JWTs; enforce org access
- Fetch permission model; mint internal JWT (60s)
- Route to backend services; add X-Org-Id, optional X-Company-Id, and correlation headers
- Apply rate limiting and CORS
- Emit traces/metrics/logs; expose health and metrics

## Key Endpoints
- /.well-known/jwks (internal JWT keys)
- /health/live, /health/ready, /metrics

## Request Flow
1. Validate external token
2. Verify org in path ∈ user.orgs
3. Fetch permissions for user+org
4. If the request includes a company context (path or header), forward `X-Company-Id`
5. Mint internal token; forward to service with X-Org-Id (and X-Company-Id when applicable)
6. Service validates token and authorizes action (including company-scoped checks)

Notes:
- The gateway does not need to validate company↔organization relationships or company-scoped permissions; services enforce those. An optional early-reject can be introduced later.

## Security
- Internal issuer: https://gateway.local, audience: internal-services
- Monthly key rotation with 2-key overlap
- Only gateway-minted tokens accepted by services

## Observability
- OTel tracing and metrics
- Structured logs with traceId, orgId, userId, companyId (when present)
- Rate limit metrics and auth failures
