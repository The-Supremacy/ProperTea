# Glossary

## Core
- Organization: Tenant boundary; holds companies and users
- Company: Business entity within an organization
- User Group: Per-organization grouping granting permissions
- Permission: Action-based capability (e.g., edit-properties)
- Company-Scoped Permission: A permission whose effect is limited to a specific set of companyIds within an organization; contrasted with org-wide permissions

## Security
- External JWT: Issued by Identity to FE (10m)
- Internal JWT: Minted by Gateway (60s) for services; includes permissionsByService and optional permissionScopes
- Zero Trust: Each service validates tokens; no implicit trust
- WORM: Write Once Read Many, for immutable audit logs

## Architecture
- API Gateway (YARP): Single public entry; auth, routing, rate limits
- Outbox Pattern: Store-and-forward events with reliability
- Saga: Orchestrated multi-step workflow with compensations

## Observability
- OpenTelemetry: Standard tracing/metrics/logs
- Correlation/Trace ID: Request flow identifier
- RFC 7807: Problem+JSON error response format

## Messaging
- CloudEvents: Standard event envelope
- DLQ: Dead-letter queue/topic for poison messages
- Idempotency: Safe reprocessing without side effects

## Conventions
- Routes: /api/v0/organization/{orgId}/service/...
- Headers: X-Org-Id, X-Company-Id (when applicable), X-Correlation-ID
- Topic Naming: domain.service.event.v1
