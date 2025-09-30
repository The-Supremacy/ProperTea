# ADR-005: Authorization and Permissions Model

## Status
Accepted

## Context
We need fine-grained, action-based permissions that can span multiple services, while remaining simple and performant. Users can belong to multiple organizations, with permissions granted via groups per organization. Some business rules depend on resource state and should be enforced within services. Certain permissions should be constrained to specific companies inside an organization.

## Decision
- Use an action-based permission model grouped by service domain: organizations, companies, userManagement, property, shared.
- User’s effective permissions = union of all group-assigned permissions within the organization.
- Permissions can be:
  - Org-wide: apply to all companies in the organization
  - Company-scoped: restricted to a set of companyIds within the organization
- Gateway enforces organization access; services perform local checks for required actions from internal JWT.
- Company-scoped enforcement: services must ensure the target resource’s companyId is included in the permission’s allowed companies when `scopeType=company`.
- Complex ABAC-style checks tied to resource state are enforced within the owning service (service-local).
- Critical flows may perform a lightweight recheck with Authorization Service when needed.
- Authorization Service provides a single endpoint: GET /auth/user/{userId}/org/{orgId}/permissions-model that includes both permissionsByService and per-permission scoping metadata.

## Consequences

Positive:
- Clear and explicit permission semantics with company-level control
- High performance with mostly local checks using internal token
- Decoupled from centralized policy engines; can adopt OPA later if necessary

Negative:
- Token size grows with company-scoped details
- Services must handle scoping checks (resource → company)

Mitigations:
- Keep permissions grouped by domain for clarity
- Prefer org-wide permissions where possible to limit token size
- Optionally evolve to versioned/hashed permission model later
