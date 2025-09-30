# Organizations Service

## Purpose
Manage organizations as the tenant boundary and store tenant-level settings and feature flags.

## Responsibilities
- Create, update, archive organizations
- Feature flags: org defaults and group overrides (through Authorization/Feature services)
- Org-wide settings (contact info, branding, defaults)
- Expose org directory and search for user onboarding flows

## API
- `GET /organizations/{orgId}`
- `PUT /organizations/{orgId}`
- `POST /organizations` (create)
- `POST /organizations/{orgId}/archive`
- `GET /organizations` (list/search)

## Multitenancy
- Organization is the tenant key
- Most downstream services are scoped by organization

## Observability
- Metrics: active orgs, archived orgs
- Traces: org lifecycle operations
- Audit: all org changes, archival
