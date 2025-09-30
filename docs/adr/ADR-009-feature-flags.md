# ADR-009: Feature Flag Management

## Status
Accepted

## Context
We need to toggle features per organization and per group, supporting hierarchical Feature → Function flags, while staying simple and .NET-native.

## Decision
- Use Microsoft.FeatureManagement with a custom IFeatureDefinitionProvider backed by Postgres.
- Flags are evaluated with organization context (and optionally group) and may be cached in Redis.
- UI supports convenience toggles (feature-level selects all functions) but stores explicit Function-level flags for groups.
- Future: integrate Azure App Configuration or OpenFeature if needed.

## Consequences

Positive:
- Simple and native to .NET
- Supports org and group scoping with minimal code
- Easy to evolve to centralized config later

Negative:
- Custom provider required
- No hosted management UI initially

Mitigations:
- Provide simple admin endpoints and audit flag changes
