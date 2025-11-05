# Authentication & Authorization Strategy

**Version:** 1.2.0  
**Last Updated:** October 30, 2025  
**Status:** MVP 1 Specification - Revised

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication Architecture](#authentication-architecture)
3. [Authorization Architecture](#authorization-architecture)
4. [Session Management](#session-management)
5. [JWT Structure & Enrichment](#jwt-structure--enrichment)
6. [Permission Model](#permission-model)
7. [Multi-Tenancy & Organization Context](#multi-tenancy--organization-context)
8. [External Identity Providers](#external-identity-providers)
9. [Security Considerations](#security-considerations)
10. [GDPR & Data Privacy](#gdpr--data-privacy)

---

## Overview

ProperTea implements a **hybrid authentication model**:
- **Session-based** authentication for web clients (HTTP-only cookies)
- **JWT-based** authentication for internal service-to-service communication
- **Permission-based** authorization (not role-based)
- **Multi-tenant** with organization-scoped permissions

### Key Principles

1. **Security First:** JWTs never exposed to clients (stored in BFF sessions)
2. **Single Sign-On:** One account works across all portals (Landlord, Tenant, Market)
3. **Organization Switching:** Users can switch between orgs they belong to
4. **Fine-Grained Permissions:** Group-based permissions at org or company scope
5. **Extensible:** Supports local accounts, Google OAuth, Azure Entra ID

---

## Authentication Architecture

### Components

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ POST /api/auth/login {email, password}
       │ Cookie: properteasession=<sessionId>
       ▼
┌──────────────┐
│ Landlord BFF │ (Session Gateway)
│              │
│ 1. Validates session cookie
│ 2. Fetches JWT from Redis
│ 3. Enriches JWT with org permissions (on-demand)
│ 4. Forwards to downstream services
└──────┬───────┘
       │ Authorization: Bearer <enriched-jwt>
       ▼
┌────────────────┐      ┌──────────────┐      ┌────────────────┐
│ Identity       │      │ Permission   │      │ Contact        │
│ Service        │      │ Service      │      │ Service        │
│                │      │              │      │                │
│ - User auth    │      │ - Groups     │      │ - Profiles     │
│ - JWT creation │      │ - Permissions│      │ - GDPR         │
└────────────────┘      └──────────────┘      └────────────────┘
```

### Authentication Flow (Login)

**Step 1: User Submits Credentials**
```http
POST https://landlord.propertea.com/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePassword123!"
}
```

**Step 2: BFF → Identity Service**
```
BFF calls Identity Service:
POST http://identity-service:80/api/token/login
{
  "email": "alice@example.com",
  "password": "SecurePassword123!"
}

Identity Service:
1. Validates credentials (UserManager.CheckPasswordSignInAsync)
2. Checks lockout status
3. Updates last login timestamp
4. Generates JWT with basic claims:
   - sub: userId (Guid)
   - email: alice@example.com
   - exp: 15 minutes from now
   - iss: ProperTea.Identity
   - aud: ProperTea

Response:
{
  "userId": "guid",
  "email": "alice@example.com",
  "accessToken": "eyJhbGc..."
}
```

**Step 3: BFF Creates Session**
```
BFF:
1. Generates sessionId: "session:guid"
2. Creates UserSession object with basic user info (NO enriched JWTs yet)
   {
     "sessionId": "session:guid",
     "userId": "guid",
     "userEmail": "alice@example.com",
     "organizationJwts": {}, // Initially empty - enriched on-demand
     "createdAt": "2025-10-30T10:00:00Z",
     // ...
   }
3. Stores in Redis with 7-day TTL
4. Sets HTTP-only cookie:
   Set-Cookie: properteasession=session:guid; 
               HttpOnly; Secure; SameSite=Strict; 
               MaxAge=604800
```

**Step 4: Response to Client**
```http
HTTP/1.1 200 OK
Set-Cookie: properteasession=session:guid; HttpOnly; Secure; SameSite=Strict
Content-Type: application/json

{
  "message": "Login successful."
}
```

**Client never sees JWT!** Only session cookie.

---

## Authorization Architecture

### Permission Flow (On-Demand JWT Enrichment)

```
┌─────────────┐
│   Browser   │ GET /org/{orgId}/properties
└──────┬──────┘
       │ Cookie: properteasession=<sessionId>
       ▼
┌──────────────────────────┐
│ Landlord BFF             │
│                          │
│ 1. Validates session     │
│ 2. Extracts orgId        │
│ 3. Checks session cache for orgId's JWT │
│ 4. IF NOT FOUND:         │
│    a. Call Permission Service for perms │
│    b. Create enriched JWT │
│    c. Store new JWT in session cache │
│ 5. Forward JWT to service│
└──────┬───────────────────┘
       │ Authorization: Bearer <enriched-jwt-for-orgId>
       ▼
┌────────────────┐
│ Property Base  │
│ Service        │
│                │
│ 1. Validates JWT
│ 2. Extracts claims:
│    - userId
│    - orgId: org1
│    - permissions: ["Property.View", "Property.Manage"]
│ 3. Checks if "Property.View" in permissions
│ 4. Filters data by orgId
│ 5. Returns properties
└────────────────┘
```

### JWT Enrichment (Organization Context)

**Base JWT from Identity Service:**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "exp": 1729598400,
  "iss": "ProperTea.Identity",
  "aud": "ProperTea"
}
```

**Enriched JWT (Created on-demand by BFF):**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "orgId": "org1-guid",
  "orgName": "Acme Property Management",
  "orgOwnerUserId": "owner-guid",
  "groups": ["PropertyManager", "LeaseApprover"],
  "permissions": {
    "Property.View": [],
    "Property.Manage": ["comp1-guid"],
    "Lease.View": ["comp1-guid", "comp2-guid"],
    "Organization.Manage": []
  },
  "cacheVersion": 5,
  "exp": 1729598400,
  "iss": "ProperTea.Identity",
  "aud": "ProperTea"
}
```

**Authorization Check with Scoped Permissions:**

```csharp
// Property Base Service - Authorization Middleware
public void CheckAccess(string requiredPermission, Guid requiredCompanyId)
{
    var permissionsClaim = _httpContext.User.FindFirst("permissions");
    if (permissionsClaim == null) throw new ForbiddenException("No permissions claim found.");

    var permissions = JsonSerializer.Deserialize<Dictionary<string, string[]>>(permissionsClaim.Value);

    // 1. Check if the user has the permission at all
    if (!permissions.TryGetValue(requiredPermission, out var allowedCompanyIds))
    {
        throw new ForbiddenException($"Missing required permission: {requiredPermission}");
    }

    // 2. Check the scope of the permission
    // An empty array means organization-wide access.
    bool isOrgWide = allowedCompanyIds.Length == 0;
    if (isOrgWide)
    {
        return; // Access granted (org-wide)
    }

    // 3. If not org-wide, check if the required company is in the allowed list
    if (!allowedCompanyIds.Contains(requiredCompanyId.ToString()))
    {
        throw new ForbiddenException($"Permission '{requiredPermission}' not granted for company '{requiredCompanyId}'.");
    }

    // Access granted
}

// Usage:
CheckAccess("Property.Manage", propertyToUpdate.CompanyId);
```

**How BFF Enriches JWT (On-Demand):**

```csharp
// BFF Session Management Middleware
public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, IOrganizationService orgService)
{
    // 1. Get session from Redis
    var session = await GetSessionAsync(sessionId);
    
    // 2. Extract orgId from request URL
    var orgId = ExtractOrgId(context);
    
    // 3. Check session cache for an existing enriched JWT for this org
    if (!session.OrganizationJwts.TryGetValue(orgId, out var enrichedJwt) || IsJwtExpiringSoon(enrichedJwt))
    {
        // 4. If not found or expiring, enrich on-demand
        var org = await orgService.GetAsync(orgId); // Get org details
        var permissions = await permissionService.GetUserPermissionsAsync(session.UserId, orgId);
        
        // 5. Create new enriched JWT, including the orgOwnerUserId
        enrichedJwt = CreateEnrichedJwt(session.UserId, org, permissions);
        
        // 6. Update the session cache in Redis with the new JWT
        session.OrganizationJwts[orgId] = enrichedJwt;
        await UpdateSessionAsync(session);
    }
    
    // 7. Add the correct JWT to the Authorization header
    context.Request.Headers.Authorization = $"Bearer {enrichedJwt}";
    
    await _next(context);
}
```

---

## Session Management

### Session Structure (On-Demand Enrichment)

**Stored in Redis:**
```json
{
  "sessionId": "session:guid",
  "userId": "user-guid",
  "userEmail": "alice@example.com",
  "organizationJwts": {
    // Populated on-demand as user accesses different orgs
    "org1-guid": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "org2-guid": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  },
  "createdAt": "2025-10-30T10:00:00Z",
  "lastRefreshedAt": "2025-10-30T10:15:00Z",
  "expiresAt": "2025-11-06T10:00:00Z",
  "deviceInfo": {
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)..."
  }
}
```

**Redis Key:** `session:{sessionId}`  
**TTL:** 7 days (sliding expiration - refreshed on each request)

**Multi-Organization Support (On-Demand):**
- Session is created with an **empty** `organizationJwts` map at login.
- On first request to an organization (`/org/{orgId}/...`), the BFF enriches and caches the JWT for that specific `orgId`.
- Subsequent requests to the same `orgId` use the cached JWT, making them fast.
- This avoids a slow login if a user belongs to many organizations.

### Session Lifecycle

**1. Creation (Login) - Simplified**
```csharp
// User authenticates
var loginResult = await _identityService.LoginAsync(email, password);

// Store session in Redis with ONLY user info
var session = new Session
{
    SessionId = Guid.NewGuid(),
    UserId = loginResult.UserId,
    UserEmail = email,
    OrganizationJwts = new Dictionary<string, string>(), // Empty - enriched on-demand
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddDays(7)
};

await _redis.SetAsync($"session:{session.SessionId}", session, TimeSpan.FromDays(7));
```

**2. Validation & On-Demand Enrichment (Every Request)**
```csharp
// SessionManagementMiddleware
public async Task InvokeAsync(HttpContext context, IDistributedCache cache, IPermissionService permissionService, IOrganizationService orgService)
{
    if (!context.Request.Cookies.TryGetValue("properteasession", out var sessionId))
    {
        await _next(context);
        return;
    }
    
    // Get session from Redis
    var sessionJson = await cache.GetStringAsync($"session:{sessionId}");
    if (sessionJson == null)
    {
        // Session expired or invalid
        context.Response.StatusCode = 401;
        return;
    }
    
    var session = JsonSerializer.Deserialize<Session>(sessionJson);
    
    // Extract orgId from URL path: /org/{orgId}/...
    var orgId = ExtractOrgIdFromPath(context.Request.Path);
    
    if (orgId == null) { /* ... no org context needed ... */ }
    
    // Look up pre-enriched JWT for this organization
    if (!session.OrganizationJwts.TryGetValue(orgId, out var enrichedJwt) || IsJwtExpiringSoon(enrichedJwt))
    {
        // NOT IN CACHE or EXPIRING: Enrich on-demand
        var org = await orgService.GetAsync(Guid.Parse(orgId));
        var permissions = await permissionService.GetUserPermissionsAsync(session.UserId, Guid.Parse(orgId));
        enrichedJwt = CreateEnrichedJwt(session.UserId, org, permissions);
        
        // Update session cache with the newly created JWT
        session.OrganizationJwts[orgId] = enrichedJwt;
        await cache.SetStringAsync($"session:{sessionId}", JsonSerializer.Serialize(session));
    }
    
    // Forward enriched JWT to downstream services
    context.Request.Headers.Authorization = $"Bearer {enrichedJwt}";
    
    await _next(context);
}

private string? ExtractOrgIdFromPath(PathString path)
{
    // Pattern: /org/{guid}/...
    var segments = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length >= 2 && segments[0] == "org" && Guid.TryParse(segments[1], out _))
    {
        return segments[1];
    }
    return null;
}
```

**3. Organization Context via URL**
- There is no explicit "switch organization" API.
- The user's context is determined entirely by the URL they navigate to (e.g., `/org/{orgId}/dashboard`).
- The BFF middleware handles fetching or creating the correct JWT based on the URL.

**Benefits of On-Demand Enrichment:**
- ✅ **Fast Login:** Login is always fast, regardless of how many organizations a user belongs to.
- ✅ **Efficient:** Caching is done only for organizations the user actually interacts with in a session.
- ✅ **Scalable:** Handles users with hundreds of organization memberships without a slow login.

## JWT Structure & Enrichment

### Basic JWT (from Identity Service)

**Purpose:** Identify user, short-lived (15 minutes)

**Claims:**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "exp": 1729598400,
  "iat": 1729597500,
  "iss": "ProperTea.Identity",
  "aud": "ProperTea",
  "jti": "token-guid"
}
```

**Signing:** HS256 (symmetric key from configuration)

**Lifetime:** 15 minutes (short-lived, refreshed automatically by BFF)

### Enriched JWT (for Landlord Portal)

**Purpose:** Include org context and permissions for authorization

**Claims:**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "orgId": "org1-guid",
  "orgName": "Acme Property Management",
  "orgOwnerUserId": "owner-guid",
  "groups": ["PropertyManager", "LeaseApprover"],
  "permissions": {
    "Property.View": [],
    "Property.Manage": ["comp1-guid"],
    "Lease.View": ["comp1-guid", "comp2-guid"],
    "Organization.Manage": []
  },
  "cacheVersion": 5,
  "exp": 1729598400,
  "iat": 1729597500,
  "iss": "ProperTea.Identity",
  "aud": "ProperTea",
  "jti": "token-guid"
}
```

**Authorization Check with Scoped Permissions:**

```csharp
// Property Base Service - Authorization Middleware
public void CheckAccess(string requiredPermission, Guid requiredCompanyId)
{
    var permissionsClaim = _httpContext.User.FindFirst("permissions");
    if (permissionsClaim == null) throw new ForbiddenException("No permissions claim found.");

    var permissions = JsonSerializer.Deserialize<Dictionary<string, string[]>>(permissionsClaim.Value);

    // 1. Check if the user has the permission at all
    if (!permissions.TryGetValue(requiredPermission, out var allowedCompanyIds))
    {
        throw new ForbiddenException($"Missing required permission: {requiredPermission}");
    }

    // 2. Check the scope of the permission
    // An empty array means organization-wide access.
    bool isOrgWide = allowedCompanyIds.Length == 0;
    if (isOrgWide)
    {
        return; // Access granted (org-wide)
    }

    // 3. If not org-wide, check if the required company is in the allowed list
    if (!allowedCompanyIds.Contains(requiredCompanyId.ToString()))
    {
        throw new ForbiddenException($"Permission '{requiredPermission}' not granted for company '{requiredCompanyId}'.");
    }

    // Access granted
}

// Usage:
CheckAccess("Property.Manage", propertyToUpdate.CompanyId);
```

**Benefits of this model:**
- ✅ **Explicit Scopes:** The JWT clearly states which resources a permission applies to.
- ✅ **Fast & Stateless:** Downstream services remain self-sufficient for authorization.
- ✅ **Flexible:** Supports org-wide, company-specific, or even more granular scopes in the future.

### Enriched JWT (for Tenant/Market Portals)

**Purpose:** User-scoped (no org context)

**Claims:**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "exp": 1729598400,
  "iat": 1729597500,
  "iss": "ProperTea.Identity",
  "aud": "ProperTea",
  "jti": "token-guid"
}
```

**Note:** No orgId or permissions - Tenant/Market portals show user's own data across all orgs.

---

## Permission Model

### Structure

**Permissions are defined by services:**
```csharp
// Property Base Service
public static class PropertyPermissions
{
    public const string View = "Property.View";
    public const string Manage = "Property.Manage";
    public const string Create = "Property.Create";
    public const string Update = "Property.Update";
    public const string Delete = "Property.Delete";
}

public static class RentalObjectPermissions
{
    public const string View = "RentalObject.View";
    public const string Create = "RentalObject.Create";
    public const string Update = "RentalObject.Update";
    public const string Delete = "RentalObject.Delete";
    public const string Publish = "RentalObject.Publish";
}
```

**Services register permissions on startup:**
```csharp
// Property Base Service - Program.cs
await _eventBus.PublishAsync(new PermissionsRegisteredEvent
{
    ServiceName = "PropertyBase",
    Permissions = new[]
    {
        new PermissionDefinition("Property.View", "View properties", "Property Management"),
        new PermissionDefinition("Property.Manage", "Full property management", "Property Management"),
        new PermissionDefinition("RentalObject.Create", "Create rental objects", "Rental Objects"),
        // ... more permissions
    }
});
```

**Permission Service stores and caches:**
```sql
CREATE TABLE permission_definitions (
    id UUID PRIMARY KEY,
    service_name VARCHAR(100),
    permission_key VARCHAR(200) UNIQUE,
    description VARCHAR(500),
    category VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW()
);
```

### Groups (Organization or Company Scoped)

**Group Structure:**
```sql
CREATE TABLE groups (
    id UUID PRIMARY KEY,
    name VARCHAR(100),
    organization_id UUID NOT NULL,
    company_id UUID NULL,  -- NULL = org-wide, set = company-specific
    description VARCHAR(500),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE group_members (
    group_id UUID REFERENCES groups(id),
    user_id UUID NOT NULL,
    assigned_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (group_id, user_id)
);

CREATE TABLE group_permissions (
    group_id UUID REFERENCES groups(id),
    permission_key VARCHAR(200),
    granted_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (group_id, permission_key)
);
```

**Example Groups:**

**Org-wide group (applies to all companies in org):**
```json
{
  "id": "group-guid",
  "name": "Administrator",
  "organizationId": "org1-guid",
  "companyId": null,
  "permissions": [
    "Property.Manage",
    "Lease.Approve",
    "User.Manage",
    "Permission.Manage"
  ]
}
```

**Company-specific group:**
```json
{
  "id": "group-guid",
  "name": "Property Manager - Building A",
  "organizationId": "org1-guid",
  "companyId": "comp1-guid",
  "permissions": [
    "Property.View",
    "RentalObject.Create",
    "RentalObject.Update",
    "Lease.View"
  ]
}
```

**Granular Permission Scoping (Revised):**

To support scenarios like a "Regional Manager" who can view all properties in an organization but only manage properties in a specific company, the permission model must be structured.

**The Solution: Scoped Permissions in JWT**
- The flat list of permissions is replaced by a JSON object within the JWT.
- The keys of the object are the permission strings (e.g., `Property.Manage`).
- The values are arrays of resource IDs (e.g., company IDs) to which that permission applies.
- An empty array `[]` signifies **organization-wide scope**.

**Example JWT Claim:**
```json
"permissions": {
  "Property.View": [], // Can view properties across the entire organization
  "Property.Manage": ["comp1-guid"] // Can ONLY manage properties in Company 1
}
```

**Implementation:**
- The **Permission Service** is responsible for resolving a user's group memberships and roles into this final, structured permission object for a given organization.
- The **BFF** requests this structure from the Permission Service and places it directly into the `permissions` claim of the enriched JWT.
- **Downstream services** use this structure to perform detailed, scoped authorization checks as shown in the `Authorization Check with Scoped Permissions` section above. This keeps their logic simple and fast.

### Default Groups (Seeded on Organization Creation)

**Event-driven:**
```
Organization Service: OrganizationCreated event published
  ↓
Permission Worker: Listens to event, creates default groups:
  - Administrator (all permissions)
  - User (basic permissions: view own data)
```

**Default groups:**
```json
[
  {
    "name": "Administrator",
    "permissions": ["*"] 
  },
  {
    "name": "User",
    "permissions": [
      "Contact.ViewOwn",
      "Lease.ViewOwn"
    ]
  }
]
```

---

## Multi-Tenancy & Organization Context

### Multi-Organization Context via URL

**How context is determined:**
- There is **no explicit "switch organization" API**.
- The user's context is determined entirely by the URL they navigate to.
- The BFF middleware is responsible for extracting the `orgId` from the URL and ensuring the user has access.

**Option 1: Subdomain-based (Future)**
```
User navigates to: https://acme.propertea.dev
  ↓
BFF extracts orgId from subdomain mapping
```

**Option 2: Route parameter (Current)**
```
User navigates to: https://landlord.propertea.dev/org/{orgId}/properties
  ↓
BFF extracts orgId from route
```

**BFF re-enriches JWT with new org's permissions.**

### Data Isolation

**All domain tables include org discriminators:**
```sql
CREATE TABLE properties (
    id UUID PRIMARY KEY,
    company_id UUID NOT NULL,  -- Foreign key to Company
    name VARCHAR(200),
    address VARCHAR(500),
    created_at TIMESTAMP DEFAULT NOW()
);

-- Company table links to Organization
CREATE TABLE companies (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL,
    name VARCHAR(200),
    created_at TIMESTAMP DEFAULT NOW()
);
```

**Queries always filter by org/company:**
```csharp
// Property Base Service
public async Task<List<Property>> GetPropertiesAsync(Guid companyId)
{
    // JWT contains orgId and companyIds claims
    var allowedCompanyIds = _httpContext.User.FindAll("companyIds").Select(c => Guid.Parse(c.Value));
    
    if (!allowedCompanyIds.Contains(companyId))
    {
        throw new ForbiddenException("Access denied to this company");
    }
    
    return await _context.Properties
        .Where(p => p.CompanyId == companyId)
        .ToListAsync();
}
```

---

## External Identity Providers

### Supported Providers

**MVP 1:**
- Local accounts (email + password)
- Google OAuth
- Azure Entra ID (optional, production)

**Future:**
- Microsoft Account (personal)
- GitHub
- Facebook

### Integration Architecture

```
┌─────────────┐
│   Browser   │ Clicks "Login with Google"
└──────┬──────┘
       │
       ▼
┌──────────────┐
│ Landlord BFF │ Redirects to Identity Service
└──────┬───────┘
       │
       ▼
┌────────────────┐
│ Identity       │ GET /api/auth/external/google
│ Service        │   ↓
│                │ Redirects to Google OAuth
└────────┬───────┘
         │
         ▼
┌─────────────────┐
│ Google OAuth    │ User authenticates
└────────┬────────┘
         │
         ▼
┌────────────────┐
│ Identity       │ POST /api/auth/external/callback
│ Service        │   ↓
│                │ 1. Validates OAuth token
│                │ 2. Gets user info from Google
│                │ 3. Links to existing user OR creates new user
│                │ 4. Stores in ExternalLogins table
│                │ 5. Generates JWT
└────────┬───────┘
         │
         ▼
┌──────────────┐
│ Landlord BFF │ Receives JWT, creates session
└──────────────┘
```

### External Login Storage

```sql
CREATE TABLE external_logins (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    provider VARCHAR(50) NOT NULL,  -- "Google", "Entra", "Microsoft"
    provider_key VARCHAR(200) NOT NULL,  -- External user ID
    provider_display_name VARCHAR(200),
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(provider, provider_key)
);
```

**Example:**
```json
{
  "userId": "alice-guid",
  "provider": "Google",
  "providerKey": "google-oauth-id-12345",
  "providerDisplayName": "alice@gmail.com"
}
```

### Account Linking

**Scenario:** User has local account (alice@example.com), wants to link Google account.

**Flow:**
```
1. User logs in with local account
2. User navigates to profile settings
3. User clicks "Link Google Account"
4. BFF redirects to Identity: GET /api/auth/external/google?userId=alice-guid
5. Identity redirects to Google OAuth
6. User authenticates with Google
7. Identity receives callback, checks:
   - Is this Google account already linked to another user? → Error
   - Does userId match current session? → Link accounts
8. Identity creates ExternalLogin record
9. User can now login with either local or Google account
```

### Azure Entra ID (Optional, Production)

**Configuration:**
```json
{
  "EntraIdSettings": {
    "Enabled": true,
    "TenantId": "your-tenant-id",
    "ClientId": "your-app-id",
    "ClientSecret": "from-key-vault",
    "Instance": "https://login.microsoftonline.com/"
  }
}
```

**When enabled:**
- "Login with Microsoft" button appears
- Used for corporate accounts (e.g., @acmeproperty.com)
- Local accounts still available (coexist)

---

## Security Considerations

### JWT Security

**1. Never Expose JWT to Client**
- JWTs stored in BFF Redis sessions
- Clients only receive HTTP-only session cookies
- Prevents XSS attacks (JavaScript can't access JWT)

**2. Short-Lived Tokens**
- JWT lifetime: 15 minutes
- Automatically reissued by BFF before expiration
- Reduces impact of token theft

**3. Token Reissuance Validation**
```csharp
// Identity Service - Reissue endpoint
var validationParameters = new TokenValidationParameters
{
    ValidateLifetime = false,  // Accept expired tokens
    ValidateIssuerSigningKey = true,
    ValidIssuer = _config["JwtSettings:Issuer"],
    ValidAudience = _config["JwtSettings:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(...)
};

var principal = tokenHandler.ValidateToken(expiredToken, validationParameters, out _);

// Check user still exists and is active
var user = await _userManager.FindByIdAsync(principal.FindFirst("sub").Value);
if (user == null || !user.IsActive)
{
    return Unauthorized();
}

// Issue new token
var newToken = _tokenService.CreateToken(user);
```

**Future Enhancement:** Add `jti` (JWT ID) to claims, track in Redis, prevent reuse.

### Session Security

**1. Cookie Attributes**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,        // JavaScript can't access
    Secure = true,          // HTTPS only
    SameSite = SameSiteMode.Strict,  // CSRF protection
    MaxAge = TimeSpan.FromDays(7),
    Path = "/",
    Domain = ".propertea.com"  // Works across subdomains
};
```

**2. Session Binding**
- IP address stored in session (optional strict validation)
- User agent stored (detect session hijacking)

**3. Concurrent Sessions**
- Allow multiple devices (desktop, mobile)
- Each device has separate session
- User can view/revoke sessions (future feature)

### Permission Caching & Invalidation

**Cache Strategy:**
```
Redis Key: "permissions:user:{userId}:org:{orgId}:version:{version}"
TTL: 1 hour
```

**Invalidation:**
```
When permissions change (group membership, group permissions):
1. Permission Service increments cache version
2. Permission Service publishes UserPermissionsChangedEvent
3. BFF workers listen to event, invalidate local cache
4. Next request: BFF fetches fresh permissions
```

**Example:**
```csharp
// Permission Service
public async Task UpdateGroupPermissionsAsync(Guid groupId, List<string> permissions)
{
    // 1. Update database
    await _repository.UpdateGroupPermissionsAsync(groupId, permissions);
    
    // 2. Increment cache version for all users in group
    var userIds = await _repository.GetGroupMembersAsync(groupId);
    foreach (var userId in userIds)
    {
        await _cache.IncrementAsync($"permissions:version:{userId}");
        
        // 3. Publish event
        await _eventBus.PublishAsync(new UserPermissionsChangedEvent
        {
            UserId = userId,
            NewCacheVersion = newVersion
        });
    }
}
```

### Rate Limiting (Defense in Depth)

A multi-layered approach to rate limiting provides the best security.

**Layer 1: Ingress/Edge (Traefik)**
- **Responsibility:** First line of defense. Protects the entire cluster from volumetric attacks (DDoS) and broad brute-force attempts.
- **Implementation:** Traefik's rate limiting middleware.
- **Strategy:** IP-based, relatively generous limits (e.g., 1000 requests/minute per IP).
- **Benefit:** Blocks malicious traffic before it ever reaches the application services.

**Layer 2: BFF (Application-Aware)**
- **Responsibility:** Granular, user-aware rate limiting for sensitive endpoints.
- **Implementation:** ASP.NET Core Rate Limiting middleware.
- **Strategy:** User/session-based, strict limits on specific actions.
  - **Login:** 10 attempts per minute per IP.
  - **Password Reset:** 5 requests per hour per user.
- **Benefit:** Prevents abuse by authenticated or unauthenticated users that Layer 1 would miss.

This defense-in-depth strategy is robust. Traefik handles the brute force, and the BFF handles the nuanced application-level abuse.

---

## GDPR & Data Privacy

### Right to Erasure (Delete User Data)

**Orchestrated Saga:**
```
1. User requests deletion (POST /api/contact/delete-request)
   ↓
2. Contact Service starts DeletionRequestSaga
   ↓
3. Saga calls CanDeleteUser in all services:
   - Lease: Check active leases → Block if active
   - Invoice: Check unpaid invoices → Block if unpaid
   - Maintenance: Check open work orders → OK to delete (anonymize)
   ↓
4. If all checks pass:
   - Contact Service: Soft delete and anonymize the specific `Contact` record associated with that user and organization.
   - If the user has no other contacts in other organizations:
     - Identity Service: Soft delete user account.
     - Permission Service: Remove all group memberships.
   - Other services: Anonymize user references
   ↓
5. Saga completes
```

**Soft Delete:**
```sql
-- Contact Service
UPDATE personal_profiles SET
    full_name = 'DELETED USER',
    email = 'deleted_' || id || '@deleted.local',
    phone = NULL,
    is_deleted = true,
    deleted_at = NOW()
WHERE user_id = :userId;

-- Identity Service
UPDATE users SET
    email = 'deleted_' || id || '@deleted.local',
    is_active = false,
    is_deleted = true,
    deleted_at = NOW()
WHERE id = :userId;
```

**Historical Data Retention:**
- Lease agreements: Keep tenant name (anonymized after 2 years)
- Invoices: Keep for 7 years (legal requirement)
- Audit logs: Keep for 1 year (security requirement)

**Note on Org-Owned Contacts:**
With the organization-owned contact model, a GDPR deletion request is simpler. Deleting a user from one organization only removes the `Contact` associated with that org. The global `User` account is only deleted if it's their last remaining `Contact`. This provides clear data ownership and isolation.

### Data Export (Right to Portability)

**API Endpoint:**
```http
GET /api/contact/export?userId={userId}

Response: ZIP file containing:
- personal_profile.json
- organization_profiles.json
- leases.json
- invoices.json
- maintenance_requests.json
```

---

## Implementation Checklist

### Phase 1: Core Authentication
- [x] Identity Service (JWT generation, local accounts)
- [x] BFF Session Management (Redis, cookies, on-demand enrichment)
- [ ] External logins (Google, Entra)
- [ ] Permission Service (groups, permissions)
- [x] JWT enrichment with org permissions (on-demand)

### Phase 2: Authorization
- [ ] Permission registration from services
- [ ] Group management API
- [ ] Permission caching strategy
- [ ] Authorization middleware in domain services

### Phase 3: Security Hardening
- [x] Rate limiting on public endpoints (BFF and Ingress)
- [ ] Session binding (IP, user agent)
- [ ] Audit logging (authentication attempts)
- [ ] GDPR deletion saga

---

**Next Documents:**
- `02-service-specifications.md` - Detailed service designs
- `09-local-development.md` - How to test authentication flows locally

**Document Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.2.0 | 2025-10-30 | On-demand JWT enrichment clarified, date fix |
| 1.1.0 | 2025-10-29 | On-demand JWT enrichment, clarified permission scoping, defense-in-depth rate limiting |
| 1.0.0 | 2025-10-22 | Initial authentication strategy
