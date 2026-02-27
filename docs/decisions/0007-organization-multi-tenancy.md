# Organization Multi-Tenancy Architecture

**Status**: Superseded by [0018-keycloak-adoption.md](0018-keycloak-adoption.md)
**Date**: 2026-01-27
**Deciders**: Development Team

## Context

ProperTea Landlord Portal serves multiple business customers (property management companies), each requiring:
- Isolated data and user management
- Custom branding and authentication policies
- SSO integration (Azure AD, Google Workspace)
- Domain-based organization discovery
- Support for users managing multiple legal entities (LLCs) within their organization

We evaluated two primary approaches:
1. **Hard Org Switching**: Single user account with grants to multiple organizations
2. **Separate Accounts**: Distinct user account per organization

## Decision

We will use **separate user accounts per organization** with ZITADEL organizations as the isolation boundary.

### Architecture

**ZITADEL Structure:**
```
ZITADEL Instance: ProperTea

Projects (owned by ProperTea System org):
└─ "Landlord Portal"
   ├─ Applications: landlord-web (OIDC), landlord-bff (API)
   └─ Roles: org_admin, user_manager, viewer

Organizations (per customer):
├─ "Acme Holdings"
│  ├─ Users: john@acme.com, jane@acme.com, bob@acme.com
│  ├─ Domain: acme.com (verified)
│  ├─ Project Grant: "Landlord Portal" (auto-granted)
│  ├─ SSO: Azure AD (optional)
│  └─ Branding: Acme colors/logo
│
└─ "Widgets Realty"
   ├─ Users: alice@widgets.com, carol@widgets.com
   ├─ Domain: widgets.com (verified)
   ├─ Project Grant: "Landlord Portal"
   └─ Branding: Widgets colors/logo
```

**ProperTea Services:**
```
Organization Service:
├─ Maps ZITADEL orgs to local org records
├─ Stores: subscription_tier, status, settings, branding
└─ Org lifecycle: create, deactivate, archive

Company Service (multi-LLC support):
├─ Company aggregate per legal entity
├─ Multiple companies per organization
├─ Example: "Acme Holdings" org has:
│  ├─ Company: "Acme Properties LLC"
│  ├─ Company: "Beta Investments LLC"
│  └─ Company: "Gamma Realty LLC"
└─ OpenFGA manages company-level access control

Property/Unit/Lease Services:
└─ All data scoped to companies (not orgs)
```

### User Registration & Invitation Flow

**New Organization Registration:**
```
1. User visits landing page → "Register Your Organization"
2. User enters:
   - Organization name: "Acme Holdings"
   - Admin email: john@acme.com
   - Password
3. Backend:
   a. Creates ZITADEL org with admin user (single API call)
   b. ZITADEL auto-grants "Landlord Portal" project
   c. Creates local org record
   d. Creates default Company: "Acme Holdings"
   e. OpenFGA: Grants john admin role for organization
   f. OpenFGA: Grants john owner access to company
4. User redirected to /dashboard
```

**Inviting Additional Users:**
```
1. Admin clicks "Invite User"
2. Enters: jane@acme.com, role: user_manager
3. Backend calls ZITADEL InviteUser API
4. ZITADEL:
   - Creates jane@acme.com in "Acme Holdings" org
   - Sends invitation email
   - Assigns role: user_manager
5. Jane accepts invite → Account created
```

**User Already Exists (edge case):**
```
ZITADEL handles:
- jane@acme.com exists in different org?
  → Creates separate jane@acme.com account in target org
  → Both accounts coexist (different orgs)
- jane@acme.com already in same org?
  → Returns error (user already member)
```

### Domain-Based Organization Discovery

**ZITADEL Feature:**
```
User enters email: john@acme.com
↓
ZITADEL detects verified domain: acme.com
↓
Routes to: "Acme Holdings" organization
↓
Shows: Acme Holdings branding
↓
Applies: Acme Holdings password policy + SSO
↓
Token issued scoped to "Acme Holdings"
```

**Benefits:**
- Automatic org selection (no manual picker)
- Works with SSO (SAML, OIDC)
- Corporate users get company branding immediately

### URL Structure & Routing

**Frontend Routes:**
```
Public:
├─ / → Landing page
├─ /register → New org registration
└─ /auth/callback → OIDC callback

Authenticated (token-scoped):
├─ /dashboard
├─ /companies
├─ /properties
└─ /settings
```

**Token & Tenant Scoping:**
```typescript
// Token contains:
{
  "sub": "user_abc123",
  "org_id": "acme_org_uuid",
  "email": "john@acme.com"
}

// BFF extracts and forwards:
1. Extract org_id from token claims
2. Forward as header: X-Organization-Id: acme_org_uuid
3. Forward as header: X-User-Id: user_abc123

// Services validate:
1. Extract X-Organization-Id from headers
2. Set Marten tenant: session = documentStore.LightweightSession(orgId)
3. All queries automatically scoped to organization
4. OpenFGA checks for resource-level permissions
```

### Switching Organizations (ZITADEL Native)

**User with accounts in multiple orgs:**
```
john@acme.com (Acme Holdings)
john.consultant@gmail.com (Widgets Realty)

Switching:
1. User clicks "Switch Account" in UI
2. Redirect to ZITADEL: prompt=select_account
3. ZITADEL shows accounts:
   - john@acme.com (Acme Holdings)
   - john.consultant@gmail.com (Widgets Realty)
4. User selects account
5. New token issued for selected org
6. Redirect to /dashboard (now scoped to selected org)
```

**No custom switching logic needed** - ZITADEL handles account picker.

## Consequences

### Positive

* **Security Isolation**: Token compromise affects only one organization
* **GDPR Compliance**: Delete org → All user data deleted cleanly
* **Domain Discovery**: Automatic org routing via verified domains
* **SSO Per Org**: Each org configures own identity provider
* **Custom Branding**: Login page matches organization branding
* **Simpler Implementation**: ZITADEL invitation API handles all edge cases
* **Clean Lifecycle**: Org deletion cascades to users (no orphans)
* **Audit Clarity**: Per-org audit trails, no cross-contamination
* **Data Residency**: Can deploy orgs to different ZITADEL instances by region
* **Company Aggregate**: Multi-LLC support via Company service + OpenFGA

### Negative

* **Multiple Credentials**: Users in multiple orgs manage separate passwords (mitigated by SSO)
* **Account Switching**: Requires re-authentication (~1-2 seconds)
* **Email Duplication**: john@acme.com can exist in multiple orgs (by design for isolation)

### Risks / Mitigation

* **Risk**: User forgets which email they used for which org
  * **Mitigation**: Domain-based discovery (john@acme.com auto-routes to Acme Holdings)
  * **Mitigation**: ZITADEL account picker shows all logged-in accounts

* **Risk**: Competitive organizations see user's primary email
  * **Mitigation**: Users can use different emails per org (john@acme.com vs john.contractor@gmail.com)
  * **Mitigation**: Company policy can enforce separate credentials for sensitive scenarios

* **Risk**: User invited to org but uses wrong email
  * **Mitigation**: Invitation email clearly states which email address to use
  * **Mitigation**: Domain verification prevents accidental invites to wrong domain

* **Risk**: Org deactivation leaves users stranded
  * **Mitigation**: Org deactivation = soft delete (ZITADEL org disabled, data preserved)
  * **Mitigation**: Reactivation restores access without data loss

## Alternatives Considered

### Hard Organization Switching (Single Account, Multiple Grants)

**Model**: One user account (john@example.com) with grants to multiple orgs

**Pros**:
- Single password/MFA for all orgs
- Smooth switching (no re-auth)
- Easier for users with many orgs

**Cons**:
- Security: Token compromise exposes all granted orgs
- GDPR: Complex deletion (must scrub from all orgs)
- Domain discovery broken (can't auto-route to org)
- Custom validation layer needed (higher attack surface)
- Cross-org information leakage risk (ListAuthorizations exposes all orgs)
- Token roles unreliable (org_id in token doesn't match URL org)
- Must ignore token org claims and validate per-request

**Rejected**: Security and compliance risks outweigh UX benefits

### No Multi-Tenancy (Single Organization)

**Model**: All customers in one ZITADEL organization

**Pros**:
- Simplest architecture
- No org switching complexity

**Cons**:
- No data isolation
- Can't delegate user management to customers
- No per-org SSO or branding
- Compliance nightmare (GDPR, data residency)

**Rejected**: Doesn't meet B2B requirements

## Notes

- MVP launches with email/password authentication
- SSO (SAML, OIDC) added post-MVP per customer request
- Domain verification required for production orgs
- Company service provides multi-LLC support within organizations
- Contractors will have separate portal (different ZITADEL project) if needed
- B2C users (future Tenant portal) will use separate ZITADEL organization structure
