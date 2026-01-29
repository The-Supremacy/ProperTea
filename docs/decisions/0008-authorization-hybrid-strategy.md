# Authorization Strategy: ZITADEL + OpenFGA Hybrid

**Status**: Accepted
**Date**: 2026-01-27
**Deciders**: Development Team

## Context

ProperTea requires authorization at multiple levels:

1. **Organization-level**: Who can access the portal? Who can manage users?
2. **Company-level**: Which companies can a user view/edit within their org?
3. **Resource-level**: Which specific properties, units, leases can a user access?
4. **Relationship-based**: Property manager for Building A, accountant for Company B
5. **Hierarchical**: Building → Units → Leases (access flows down hierarchy)
6. **Time-based** (future): Contractor access during service request only

We evaluated three approaches:
1. **ZITADEL RBAC Only**: All authorization in ZITADEL
2. **OpenFGA Only**: All authorization in OpenFGA
3. **Hybrid**: ZITADEL for coarse-grained, OpenFGA for fine-grained

## Decision

We will use a **hybrid authorization strategy**: ZITADEL for organization membership and high-level roles, OpenFGA (Zanzibar-based ReBAC) for resource-level permissions and relationships.

### Separation of Concerns

**ZITADEL Handles:**
- Authentication (OIDC, JWT tokens)
- Organization membership (user belongs to which organization)
- Portal access verification (is user a member of this org?)

**OpenFGA Handles (Primary Authorization):**
- All permission checks (viewer, editor, owner)
- Organization-level access (admin role, org-wide permissions)
- Group-based permissions (accounting team, maintenance team)
- Company-level access (which companies in org can user see?)
- Resource permissions (which properties/units/leases?)
- Relationships:
  - User is owner/manager/viewer of company
  - User is property manager for building
  - User is member of group with specific access
- Hierarchical access (company → property → building → unit)
- Delegated permissions (temporary contractor access)

### Architecture Diagram

```
┌──────────────────────────────────────────┐
│ User Authenticates                       │
│ → ZITADEL Issues Token                   │
│   {                                      │
│     "sub": "user_abc123",                │
│     "org_id": "acme_org_uuid",           │
│     "email": "john@acme.com"             │
│   }                                      │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│ BFF Validates Token                      │
│ → Org membership verified ✅              │
│ → Passes through to service              │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│ User Requests: GET /properties/123       │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│ BFF Forwards to Property Service         │
│ Headers:                                 │
│   X-User-Id: user_abc123                 │
│   X-Organization-Id: acme_org_uuid       │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│ Property Service                         │
│ 1. Extract org_id from headers           │
│ 2. Marten sets tenant: acme_org_uuid     │
│    → All queries scoped to org ✅         │
│ 3. Check OpenFGA for property access     │
│    → Can user:john view property:123?    │
│    → OpenFGA evaluates:                  │
│      - Direct property access?           │
│      - Manager of parent company?        │
│      - Member of group with access?      │
│    → Returns: Allowed ✅                  │
│ 4. Query Marten (org-scoped)            │
│ 5. Return property data                  │
└──────────────────────────────────────────┘
```

### OpenFGA Authorization Model (MVP Phase 1)

```typescript
model
  schema 1.1

type user

type organization
  relations
    define admin: [user, group#member]

type group
  relations
    define member: [user]

type company
  relations
    define org: [organization]
    define owner: [user, group#member]
    define manager: [user, group#member]
    define viewer: [user, group#member] or manager or owner or admin from org
    define editor: [user, group#member] or manager or owner or admin from org
```

**Key Features:**
- **Simplified MVP model** - Only core types needed initially
- **Groups** - Team-based permissions (accounting team, maintenance staff)
- **Organization admin** - Org-wide permissions without ZITADEL sync
- **Company permissions** - owner > editor > viewer hierarchy
- **Future expansion** - Property, building, unit, lease types added post-MVP

**Organization Isolation:**
- Organization membership handled by Marten multi-tenancy (not FGA)
- Token contains `org_id` → Marten sets tenant → queries auto-scoped
- FGA only handles permissions within the organization
- Defense-in-depth: Services verify `org_id` from token

### Implementation Patterns

#### Database Query Filtering

Combine OpenFGA `ListObjects` with database queries for efficient filtering:

```csharp
// Get properties user can access with business logic filter
var accessiblePropertyIds = await fgaClient.ListObjects(userId, "viewer", "property");

var properties = await session.Query<Property>()
    .Where(p => accessiblePropertyIds.Contains(p.Id) && p.CompanyId == companyId)
    .ToListAsync();
```

**Performance characteristics:**
- OpenFGA returns authorized IDs (O(log n) with indices)
- PostgreSQL executes `WHERE id IN (...)` with index scan
- Business logic filters applied in same query (single roundtrip)
- Recommended: Composite index on `(Id, CompanyId)` for optimal performance

#### Organization-Level Isolation (Marten Multi-Tenancy)

#### Organization-Level Isolation (Marten Multi-Tenancy)

**Organization membership is NOT stored in OpenFGA.** Instead:

1. **ZITADEL token** contains `org_id` claim
2. **BFF extracts** `org_id` and forwards in `X-Organization-Id` header
3. **Services use Marten multi-tenancy**:
   ```csharp
   services.AddMarten(opts => {
       opts.Policies.AllDocumentsAreMultiTenanted();
   });
   ```
4. **Middleware sets tenant** from header:
   ```csharp
   var orgId = context.Request.Headers["X-Organization-Id"];
   var session = documentStore.LightweightSession(orgId);
   ```
5. **All queries auto-scoped** to organization - complete data isolation
6. **Defense-in-depth** - Services verify org_id matches token claim

**Benefits:**
- No ZITADEL → OpenFGA org membership sync required
- Database-level tenant isolation (Marten enforces per query)
- Simpler FGA model (only permissions, not membership)
- Token is source of truth for org access

**OpenFGA handles:**
- Org admin role (without needing membership tuples)
- Company-level permissions within the organization
- Group memberships and group-based access

### Permission Management UI

**Contextual UI** (on resource pages):

Property permissions are managed directly on each resource (property, building, etc.) using inline access management components. Users with appropriate permissions can grant/revoke access to other users or groups.

Example workflows:
- Property owner can assign property managers
- Org admin can create groups and assign group-based permissions
- Company manager can grant accountant access to financial data

## Consequences

### Positive

* **Separation of Concerns**: ZITADEL for auth/org membership, OpenFGA for all authorization
* **Scalability**: OpenFGA designed for billions of tuples, handles complex queries efficiently
* **Flexibility**: Models complex relationships (groups, delegated access)
* **Database-Level Isolation**: Marten multi-tenancy provides automatic org-scoping
* **No Org Sync Required**: ZITADEL token is source of truth, no FGA sync needed
* **Group-Based Access**: Teams (accounting, maintenance) with shared permissions
* **Query Performance**: "List all accessible resources" is efficient via `ListObjects`
* **Future-Proof**: Easy to add hierarchical resources (property → building → unit)
* **Audit Trail**: OpenFGA maintains history of permission changes
* **Type Safety**: OpenFGA model is versioned and validated
* **Industry Standard**: Zanzibar-based (Google's proven authorization system)

### Negative

* **Dual Systems**: Two authorization layers (Marten tenancy + OpenFGA permissions)
* **Learning Curve**: Team must understand ReBAC (relationship-based access control) concepts
* **Operational Complexity**: Two systems to monitor (ZITADEL + OpenFGA)
* **Latency**: API call to OpenFGA per request (mitigated by caching and batch queries)
* **Custom UI Required**: Must build permission management UI (OpenFGA has no built-in admin interface)

### Risks / Mitigation

* **Risk**: OpenFGA single point of failure
  * **Mitigation**: Deploy OpenFGA with high availability (3+ replicas)
  * **Mitigation**: Cache frequently-checked permissions (5-minute TTL)
  * **Mitigation**: Fallback: If OpenFGA down, allow org_admins only

* **Risk**: Performance degradation with large datasets
  * **Mitigation**: OpenFGA designed for scale (Google Zanzibar proven at billions of tuples)
  * **Mitigation**: Use `ListObjects` batch queries instead of individual `Check` calls
  * **Mitigation**: Cache permission lists with short TTL (invalidate on changes)
  * **Mitigation**: Database composite indices on `(Id, ForeignKey)` for efficient filtering

* **Risk**: Complex authorization model is hard to debug
  * **Mitigation**: OpenFGA playground for testing authorization model
  * **Mitigation**: Comprehensive logging of permission checks
  * **Mitigation**: Admin UI to visualize user's effective permissions

## Alternatives Considered

### ZITADEL RBAC Only

**Approach**: Store all permissions in ZITADEL roles

**Pros**:
- Single authorization system
- Roles in JWT token (no extra API calls)
- Simpler architecture

**Cons**:
- Can't model resource-level permissions (property #123)
- No relationship-based access (manager of Building A)
- Can't query "all properties user can access" efficiently
- Token becomes huge (all permissions in JWT)
- Must re-authenticate when permissions change

**Rejected**: ZITADEL RBAC too coarse-grained for real estate domain

### OpenFGA Only

**Approach**: Store org membership and all permissions in OpenFGA

**Pros**:
- Single source of truth
- Consistent permission model
- Simpler sync

**Cons**:
- Duplicates ZITADEL org membership (data duplication)
- Loses ZITADEL features (SSO, domain discovery, branding)
- Must sync every ZITADEL user change to OpenFGA immediately
- Higher coupling to OpenFGA

**Rejected**: Loses value of ZITADEL; org membership is ZITADEL's strength

### Attribute-Based Access Control (ABAC)

**Approach**: Policies based on user attributes, resource attributes, context

**Pros**:
- Very flexible
- Can express complex rules

**Cons**:
- Hard to reason about ("Why does user have access?")
- Performance issues (policy evaluation expensive)
- No "list accessible resources" query
- Difficult to audit

**Rejected**: ReBAC (OpenFGA) better fit for relationship-heavy domains

## Notes

- **MVP Phase 1**: Simplified FGA model (user, group, organization, company)
- **Authorization location**: Services check OpenFGA (not BFF)
- **Organization isolation**: Marten multi-tenancy from token `org_id` (not FGA)
- **No sync required**: Token is source of truth for org membership
- **Groups**: Team-based permissions within organization
- **Future expansion**: Add property, building, unit, lease types post-MVP
- **OpenFGA model versioned**: `authorization-model.fga` in codebase
- **Team training**: Zanzibar/ReBAC concepts and OpenFGA API
