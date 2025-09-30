# Security Model

## Overview
Defense-in-depth with zero-trust between services. Gateway brokers authentication and mints short-lived internal tokens; services validate tokens and enforce permissions locally.

## Trust Boundaries
- External: Frontend ↔ Gateway over HTTPS
- Internal: Gateway ↔ Services using internal JWT (60s)
- Data: Postgres (tenant-discriminated), Redis, Kafka

## Authentication
- Identity Service issues access (10m) and refresh (30d) tokens to FE
- Gateway validates external JWT, enforces org access, fetches permissions, mints internal JWT (iss=https://gateway.local, aud=internal-services, exp=60s)
- Services accept and validate only gateway-minted internal tokens

## Authorization
- Feature-based permissions grouped by domain (organizations, companies, userManagement, property, shared)
- Permissions may be org-wide or company-scoped
- Gateway checks org access; services check required action and, when company-scoped, verify the target companyId is allowed
- Critical flows may recheck with Authorization Service
- Resource-state (ABAC-like) rules enforced in owning services

## Context Propagation
- X-Org-Id: forwarded by gateway for tenant-aware routes
- X-Company-Id: forwarded when present in path or explicitly supplied by clients; services must validate consistency and enforce company-scoped permissions

## Key Management
- Identity keys: RSA-256, quarterly rotation with overlap
- Gateway internal keys: RSA-256, monthly rotation, last 2 keys active, JWKs published
- Data Protection keys: stored in Redis, per-environment

## Rate Limiting and CORS
- Gateway token bucket keyed by (route, userId, orgId|none, IP)
- CORS at Gateway
- Request size and timeouts enforced

## RFC 7807 Errors
- application/problem+json with type, title, status, detail, instance
- Extensions: traceId, organizationId, userId, companyId (when applicable)
- Standardized across gateway and services

## Support and Emergency Access
- Support has cross-org but limited permissions; PII restrictions
- Emergency grants auto-expire after 24h; ticket/reason required; fully audited

## Network Security
- Local TLS via Traefik/mkcert
- Prod TLS via Azure front ends; mTLS optional later
- Private networking for DB/cache in prod
