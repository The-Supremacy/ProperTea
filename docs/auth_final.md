# ProperTea — Landlord Portal Authentication & Architecture

> **Document Type:** AI-Consumable Architecture Reference
> **System:** Landlord Portal (B2B/ERP)
> **Stack:** Angular 21 + PrimeNG + .NET 9 BFF + YARP + ZITADEL
> **Infrastructure:** AKS (Prod), Talos (SIT), Docker Compose (Dev)
> **Last Updated:** December 17, 2025
> **Status:** Ready for Implementation

---

## Quick Reference

| Aspect | Decision |
|--------|----------|
| **IdP** | ZITADEL |
| **Auth Pattern** | BFF (Backend for Frontend) - SPA never sees tokens |
| **Session Storage** | Redis with custom `ITicketStore` |
| **Cookie Name** | `landlord-session` |
| **UI Library** | PrimeNG |
| **Profile Management** | Native ZITADEL UI (redirect to `/ui/console/users/me`) |
| **Reverse Proxy** | YARP (BFF) + Traefik (Gateway) |
| **User Self-Deletion** | Disabled in ZITADEL; custom saga for GDPR compliance |

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    landlord.propertea.localhost                     │
├─────────────────────────────────────────────────────────────────────┤
│                            Traefik                                  │
├────────────────┬────────────────┬───────────────────────────────────┤
│   /bff/*       │    /api/*      │              /*                   │
│      ↓         │       ↓        │               ↓                   │
│    BFF         │   YARP Proxy   │      Angular (CDN)                │
│  (Auth)        │  (Downstream)  │                                   │
└──────┬─────────┴───────┬────────┴───────────────────────────────────┘
       │                 │
       ▼                 ▼
┌──────────────┐  ┌──────────────┐
│   ZITADEL    │  │  Downstream  │
│    (IdP)     │  │   Services   │
└──────────────┘  └──────────────┘
       │
       ▼
┌──────────────┐
│    Redis     │
│  (Sessions)  │
└──────────────┘
```

### Design Principles

1. **Token Isolation:** Browser never sees access/refresh tokens
2. **Same-Site Cookies:** FE and BFF share host for secure cookie handling
3. **Stateless BFF Scaling:** Sessions in Redis enable horizontal scaling
4. **ZITADEL as Source of Truth:** User identity, profiles, organizations managed in ZITADEL

---

## 2. Project Structure

### Repository Paths

```
src/landlord/portal/
├── bff/
│   └── ProperTea.Landlord.Bff/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── ProperTea.Landlord.Bff.csproj
└── web/
    └── (Angular 21 + PrimeNG project)
```

### Docker Compose Files (ops/local-dev/)

| File | Purpose |
|------|---------|
| `docker-compose.infra.yml` | Redis, PostgreSQL |
| `docker-compose.landlord.yml` | Landlord BFF + Web |
| `docker-compose.platform.yml` | ZITADEL, shared services |
| `docker-compose.tools.yml` | Dev utilities |

---

## 3. ZITADEL Organization Strategy

### Organization Hierarchy

```
ZITADEL Instance
├── ProperTea               (Internal admins/support - system operations)
├── ProperTea Users         (All registered users - stable identity home)
├── [Landlord Inc]          (B2B Customer Org - owns properties)
├── [Property Co]           (B2B Customer Org)
└── [Service Provider Orgs] (Multi-org contractors like "Inspecto")
```

### Grant-Based Access Model

**Problem Solved:** Users (e.g., contractors) may need access to multiple landlord organizations without duplicating accounts.

**Solution:**

- All users "live" in **ProperTea Users** (stable identity)
- Access to B2B orgs granted via **role grants**
- Organization context stored in **BFF session**

### Role Definitions

| Role | Description | Portal Access |
|------|-------------|---------------|
| `landlord_owner` | Owns/administers a B2B organization | Landlord Portal (full) |
| `landlord_member` | Employee of B2B organization | Landlord Portal (scoped) |
| `landlord_external` | Service provider with multi-org access | Landlord Portal (multi-org) |
| `tenant` | Renter using tenant portal | Tenant Portal |
| `market_user` | Public marketplace user | Market Portal |
| `admin` | ProperTea system administrator | Admin Portal |

### Context Switching Implementation

```
Session State (Redis):
{
  "user_id": "user-123",
  "home_org": "ProperTea Users",
  "active_org": "Landlord Inc",      // Currently selected org
  "available_orgs": ["Landlord Inc", "Property Co"],
  "roles": ["landlord_owner", "landlord_external"]
}
```

**Downstream Services:** BFF injects `X-Organization-ID` header based on `active_org` in session.

---

## 4. ZITADEL Configuration

### Project Setup

**Single Project:** `ProperTea`

**Applications:**

| App Name | Type | PKCE | Redirect URI |
|----------|------|------|--------------|
| `landlord-portal-bff` | Web (OIDC) | Required | `https://landlord.propertea.localhost/bff/callback` |
| `tenant-portal-bff` | Web (OIDC) | Required | (future) |
| `admin-portal-bff` | Web (OIDC) | Required | (future) |

### Critical Security Settings

#### Disable User Self-Deletion

**Location:** ZITADEL Console → Settings → Login Policy (or Organization Policy)

**Setting:** `Allow user to delete their own account` → **DISABLED**

**Rationale:**

- Preserves invoice/lease data integrity
- Enables GDPR-compliant deletion workflow via controlled saga
- Prevents bypass of business rule validations (unpaid invoices, active leases)

---

## 5. GDPR & User Data Management

### Data Retention Requirements

Users cannot self-delete because:

1. **Financial Records:** Invoices must be retained for legal/tax purposes (typically 7+ years)
2. **Contractual Data:** Lease agreements, property records require audit trails
3. **Multi-party Data:** User data may be referenced by multiple organizations

### GDPR-Compliant Deletion Workflow

```
User Request → Portal "Request Account Closure" Button
                            │
                            ▼
              ┌─────────────────────────┐
              │   Deletion Saga (BFF)   │
              └───────────┬─────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────┐
│ Check Unpaid  │ │ Check Active  │ │ Check Pending │
│   Invoices    │ │    Leases     │ │   Disputes    │
└───────┬───────┘ └───────┬───────┘ └───────┬───────┘
        │                 │                 │
        └─────────────────┼─────────────────┘
                          │
                    ┌─────┴─────┐
                    │           │
                    ▼           ▼
             [Blockers]    [No Blockers]
                 │              │
                 ▼              ▼
           Return Error    Execute Deletion
           with Details         │
                                ▼
                    ┌───────────────────────┐
                    │ 1. Anonymize PII      │
                    │ 2. Retain legal data  │
                    │ 3. Deactivate ZITADEL │
                    │ 4. Notify user        │
                    └───────────────────────┘
```

### Data Handling by Category

| Data Type | Retention | On Deletion Request |
|-----------|-----------|---------------------|
| **Identity (name, email)** | Until deletion | Anonymize (hash/pseudonymize) |
| **Invoices** | 7+ years (legal) | Retain with anonymized reference |
| **Lease Agreements** | Contract period + 6 years | Retain with anonymized reference |
| **Audit Logs** | Per retention policy | Retain (no PII after anonymization) |
| **Session Data** | 24h sliding | Delete immediately |
| **Preferences** | Until deletion | Delete |

### GDPR Rights Implementation

| Right | Implementation |
|-------|----------------|
| **Access (Art. 15)** | `/bff/user/data-export` endpoint |
| **Rectification (Art. 16)** | Via ZITADEL Console profile editing |
| **Erasure (Art. 17)** | Deletion Saga with business rule checks |
| **Portability (Art. 20)** | `/bff/user/data-export` (JSON format) |
| **Objection (Art. 21)** | Contact support / preferences in portal |

---

## 9. URL Routing Structure

| URL Pattern | Destination | Auth Required |
|-------------|-------------|---------------|
| `landlord.propertea.localhost/` | Angular SPA | No (app handles) |
| `landlord.propertea.localhost/bff/login` | BFF - OIDC initiation | No |
| `landlord.propertea.localhost/bff/callback` | BFF - OIDC callback | No |
| `landlord.propertea.localhost/bff/logout` | BFF - Logout | Yes |
| `landlord.propertea.localhost/bff/user` | BFF - User info | Yes |
| `landlord.propertea.localhost/bff/org/switch` | BFF - Org switching | Yes |
| `landlord.propertea.localhost/api/**` | YARP → Downstream | Yes |
| `auth.propertea.localhost/**` | ZITADEL | N/A |

---

## 10. Implementation Checklist

### Phase 1: Infrastructure

- [ ] Add Redis to `docker-compose.infra.yml`
- [ ] Add host entries for `*.propertea.localhost`
- [ ] Verify Traefik TLS certificates are trusted
- [ ] Create `docker-compose.landlord.yml`

### Phase 2: ZITADEL Configuration

- [ ] Create project "ProperTea"
- [ ] Create organization "ProperTea Users"
- [ ] Create application "landlord-portal-bff" (Web, OIDC, PKCE)
- [ ] Configure redirect URIs
- [ ] **CRITICAL:** Disable user self-deletion in Login Policy
- [ ] Define project roles

### Phase 3: BFF Development

- [ ] Create `ProperTea.Landlord.Bff` project
- [ ] Add NuGet packages
- [ ] Implement `RedisTicketStore`
- [ ] Configure OIDC authentication
- [ ] Add BFF endpoints (`/bff/login`, `/bff/logout`, `/bff/user`)
- [ ] Configure YARP with token forwarding
- [ ] Add GDPR endpoints (data export, deletion request)

### Phase 4: Angular Frontend

- [ ] Create Angular 21 project with PWA
- [ ] Install and configure PrimeNG
- [ ] Implement `AuthService` with signals
- [ ] Implement `AuthGuard`
- [ ] Implement `AuthInterceptor`
- [ ] Implement `UpdateService` for PWA
- [ ] Wire profile button to ZITADEL Console redirect

### Phase 5: Integration Testing

- [ ] Test login flow end-to-end
- [ ] Test logout with session cleanup
- [ ] Test token refresh (silent)
- [ ] Test organization switching
- [ ] Test PWA update notification
- [ ] Test GDPR data export

---

## 11. Security Considerations

### Cookie Security

| Attribute | Value | Purpose |
|-----------|-------|---------|
| `Name` | `__Host-landlord-session` | `__Host-` prefix enforces Secure + Path=/ |
| `HttpOnly` | `true` | Prevents XSS token theft |
| `SameSite` | `Strict` | CSRF protection |
| `Secure` | `true` | HTTPS only |

### Token Security

- Access tokens **never** sent to browser
- Refresh tokens stored server-side in Redis (encrypted)
- Token refresh handled by OIDC middleware automatically
- Session tickets encrypted with Data Protection API

### CSRF Protection

- SameSite=Strict cookies provide primary protection
- State-changing endpoints (`/bff/logout`, `/bff/org/switch`) require POST
- Consider adding antiforgery tokens for additional protection

### TLS Considerations

**Development:** Self-signed certs with validation bypass (scoped to OIDC backchannel only)
**Production:** Proper certificates, no validation bypass, TLS 1.3

---

## 12. Open Questions / Future Considerations

1. **Organization Switching UX:** Full page reload vs. partial refresh after org switch?
2. **Multi-tab Session Sync:** Broadcast session changes across tabs?
3. **Offline Support:** Which features should work offline (errands app)?
4. **Push Notifications:** Timeline for VAPID key setup and backend integration?
5. **Custom Profile Fields:** When to add BFF proxy for extended user attributes?

---

## Appendix A: ZITADEL Claim Mappings

| ZITADEL Claim | Mapped To | Description |
|---------------|-----------|-------------|
| `sub` | `ClaimTypes.NameIdentifier` | User ID |
| `name` | `ClaimTypes.Name` | Display name |
| `email` | `ClaimTypes.Email` | Email address |
| `urn:zitadel:iam:org:id` | `org_id` | Current organization ID |
| `urn:zitadel:iam:org:project:roles` | `org_roles` | Granted roles |

## Appendix B: Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `Auth__Authority` | ZITADEL issuer URL | `https://auth.propertea.localhost` |
| `Auth__ClientId` | OIDC client ID | `landlord-portal-bff` |
| `Auth__ClientSecret` | OIDC client secret (if confidential) | `secret` |
| `ConnectionStrings__Redis` | Redis connection string | `redis:6379` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` |
