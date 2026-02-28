# 0003: Headless Tenant Onboarding

**Status**: Partially superseded by [0018-keycloak-adoption.md](0018-keycloak-adoption.md) — registration page and programmatic provisioning retained; ZITADEL gRPC implementation and custom login container replaced
**Date**: 2026-01-22
**Deciders**: Development Team

## Context
We require a custom, branded registration experience. Using ZITADEL's default UI creates friction and limits control over the onboarding UX. We need an atomic way to provision an organization and its owner.

## Decision
The **Organization Service** will orchestrate registration using a **Reliable Handler**:
1. **Atomic Provisioning**: Use ZITADEL v2 `AddOrganization` to create the Org and the initial Human User (Administrator) in one request.
2. **Local Sync**: Persist the `OrganizationAggregate` in Marten.
3. **Eventual Consistency**: Publish an `IOrganizationRegistered` event for the User Service to create local profiles.

## Consequences
### Positive
* Fully custom registration UI in Angular.
* No "orphaned" organizations without owners.
### Negative
* Synchronous dependency on ZITADEL API during registration.
### Risks / Mitigation
* **ZITADEL Failure**: Handler returns immediate error to user.
* **Persistence Failure**: Retries are safe as ZITADEL creation is idempotent.
