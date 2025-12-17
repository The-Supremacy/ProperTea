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
| **Cookie Name** | `__Host-landlord-session` |
| **UI Library** | PrimeNG |
| **Profile Management** | Native ZITADEL UI (redirect to `/ui/console/users/me`) |
| **Reverse Proxy** | YARP (BFF) + Traefik (Gateway) |
| **User Self-Deletion** | Disabled in ZITADEL; custom saga for GDPR compliance |

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    landlord.propertea.localhost                      │
├─────────────────────────────────────────────────────────────────────┤
│                            Traefik                                   │
├────────────────┬────────────────┬───────────────────────────────────┤
│   /bff/*       │    /api/*      │              /*                   │
│      ↓         │       ↓        │               ↓                   │
│    BFF         │   YARP Proxy   │      Angular (Vite/CDN)           │
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

**Downstream Services:** BFF injects `X-Tenant-ID` header based on `active_org` in session.

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

## 6. BFF Implementation

### NuGet Packages

```xml
<PackageReference Include="Yarp.ReverseProxy" Version="2.2.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.*" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.*" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection.StackExchangeRedis" Version="9.0.*" />
<PackageReference Include="StackExchange.Redis" Version="2.8.*" />
```

### Program.cs Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. REDIS CONNECTION
// ============================================================
var redisConnection = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// ============================================================
// 2. DATA PROTECTION (Required for cookie encryption)
// ============================================================
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys")
    .SetApplicationName("ProperTea.Landlord");

// ============================================================
// 3. REDIS TICKET STORE (Sessions in Redis, not cookies)
// ============================================================
builder.Services.AddTransient<ITicketStore, RedisTicketStore>();
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

// ============================================================
// 4. AUTHENTICATION (Cookie + OIDC)
// ============================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-landlord-session";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Use Redis for session storage
    options.SessionStore = builder.Services
        .BuildServiceProvider()
        .GetRequiredService<ITicketStore>();

    // Handle auth failures gracefully for SPA
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.ClientId = builder.Configuration["Auth:ClientId"];
    options.ClientSecret = builder.Configuration["Auth:ClientSecret"]; // If confidential client
    options.ResponseType = "code";
    options.UsePkce = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");

    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    options.CallbackPath = "/bff/callback";
    options.SignedOutCallbackPath = "/bff/signout-callback";

    // Map ZITADEL claims
    options.ClaimActions.MapJsonKey("org_id", "urn:zitadel:iam:org:id");
    options.ClaimActions.MapJsonKey("org_roles", "urn:zitadel:iam:org:project:roles");

    // DEV ONLY: Bypass TLS validation for Docker networking
    if (builder.Environment.IsDevelopment())
    {
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
});

// ============================================================
// 5. YARP REVERSE PROXY
// ============================================================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        // Inject access token for downstream services
        context.AddRequestTransform(async transformContext =>
        {
            var token = await transformContext.HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            // Inject tenant context from session
            var activeOrg = transformContext.HttpContext.Session.GetString("active_org");
            if (!string.IsNullOrEmpty(activeOrg))
            {
                transformContext.ProxyRequest.Headers.Add("X-Tenant-ID", activeOrg);
            }
        });
    });

builder.Services.AddDistributedMemoryCache(); // For session state
builder.Services.AddSession();

var app = builder.Build();

// ============================================================
// 6. MIDDLEWARE PIPELINE
// ============================================================
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// 7. BFF ENDPOINTS
// ============================================================

// Login - Initiates OIDC flow
app.MapGet("/bff/login", (string? returnUrl) =>
{
    var props = new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
}).AllowAnonymous();

// Logout - Clears session and triggers OIDC logout
app.MapPost("/bff/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).RequireAuthorization();

// User Info - Returns claims for frontend
app.MapGet("/bff/user", (ClaimsPrincipal user, HttpContext ctx) =>
{
    if (user.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        IsAuthenticated = true,
        Name = user.Identity.Name,
        Email = user.FindFirstValue(ClaimTypes.Email),
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        ActiveOrg = ctx.Session.GetString("active_org"),
        Claims = user.Claims.Select(c => new { c.Type, c.Value }),
        // URL for profile management (ZITADEL native UI)
        ProfileUrl = $"{builder.Configuration["Auth:Authority"]}/ui/console/users/me"
    });
}).RequireAuthorization();

// Organization Switching
app.MapPost("/bff/org/switch", async (HttpContext ctx, SwitchOrgRequest request) =>
{
    // Validate user has access to requested org
    var userOrgs = ctx.User.FindAll("org_roles")
        .Select(c => c.Value)
        .ToList();

    if (!userOrgs.Contains(request.OrgId))
        return Results.Forbid();

    ctx.Session.SetString("active_org", request.OrgId);
    return Results.Ok();
}).RequireAuthorization();

// GDPR Data Export
app.MapGet("/bff/user/data-export", async (ClaimsPrincipal user) =>
{
    // Implementation: Gather all user data and return as JSON
    // Include: profile, preferences, activity logs (anonymized where needed)
    return Results.Ok(new { /* user data */ });
}).RequireAuthorization();

// GDPR Deletion Request
app.MapPost("/bff/user/request-deletion", async (ClaimsPrincipal user) =>
{
    // Implementation: Trigger deletion saga
    // Returns blockers if any, or confirmation if request accepted
    return Results.Accepted();
}).RequireAuthorization();

// ============================================================
// 8. YARP PROXY (catches /api/*)
// ============================================================
app.MapReverseProxy();

app.Run();

// ============================================================
// SUPPORTING TYPES
// ============================================================
public record SwitchOrgRequest(string OrgId);
```

### Redis Ticket Store Implementation

```csharp
public class RedisTicketStore : ITicketStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDataProtector _protector;
    private readonly ILogger<RedisTicketStore> _logger;
    private const string KeyPrefix = "auth-ticket:";

    public RedisTicketStore(
        IConnectionMultiplexer redis,
        IDataProtectionProvider dataProtection,
        ILogger<RedisTicketStore> logger)
    {
        _redis = redis;
        _protector = dataProtection.CreateProtector("AuthTickets");
        _logger = logger;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        await SetAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await SetAsync(key, ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var db = _redis.GetDatabase();
        var data = await db.StringGetAsync(KeyPrefix + key);

        if (data.IsNullOrEmpty)
            return null;

        try
        {
            var decrypted = _protector.Unprotect(data!);
            return TicketSerializer.Default.Deserialize(decrypted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ticket {Key}", key);
            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(KeyPrefix + key);
    }

    private async Task SetAsync(string key, AuthenticationTicket ticket)
    {
        var db = _redis.GetDatabase();
        var serialized = TicketSerializer.Default.Serialize(ticket);
        var encrypted = _protector.Protect(serialized);

        var expiry = ticket.Properties.ExpiresUtc.HasValue
            ? ticket.Properties.ExpiresUtc.Value - DateTimeOffset.UtcNow
            : TimeSpan.FromHours(8);

        await db.StringSetAsync(KeyPrefix + key, encrypted, expiry);
    }
}
```

### appsettings.json

```json
{
  "Auth": {
    "Authority": "https://auth.propertea.localhost",
    "ClientId": "landlord-portal-bff",
    "ClientSecret": ""
  },
  "ConnectionStrings": {
    "Redis": "redis:6379"
  },
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api" }
        ]
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://api-gateway:8080/"
          }
        }
      }
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Auth": {
    "Authority": "https://auth.propertea.localhost"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

---

## 7. Angular Frontend Implementation

### Project Setup Commands

```bash
cd src/landlord/portal/web
ng new propertea-landlord-portal --style=scss --routing=true --ssr=false
cd propertea-landlord-portal

# Add PWA support
ng add @angular/pwa

# Add PrimeNG
npm install primeng primeicons @primeng/themes
npm install @angular/animations @angular/cdk
```

### Auth Service

```typescript
// src/app/core/auth/auth.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, tap, of } from 'rxjs';

export interface User {
  isAuthenticated: boolean;
  name: string;
  email: string;
  userId: string;
  activeOrg: string | null;
  claims: { type: string; value: string }[];
  profileUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // Reactive state with signals
  readonly user = signal<User | null>(null);
  readonly isAuthenticated = computed(() => this.user()?.isAuthenticated ?? false);
  readonly isLoading = signal(true);

  constructor() {
    this.refreshUser();
  }

  refreshUser() {
    this.isLoading.set(true);
    this.http.get<User>('/bff/user')
      .pipe(
        tap(user => this.user.set(user)),
        catchError(() => {
          this.user.set(null);
          return of(null);
        })
      )
      .subscribe(() => this.isLoading.set(false));
  }

  login(returnUrl?: string) {
    const url = returnUrl
      ? `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`
      : '/bff/login';
    window.location.href = url;
  }

  logout() {
    // POST to ensure CSRF protection
    this.http.post('/bff/logout', {}).subscribe({
      complete: () => window.location.href = '/'
    });
  }

  // Redirect to ZITADEL native profile management
  manageProfile() {
    const profileUrl = this.user()?.profileUrl;
    if (profileUrl) {
      window.location.href = profileUrl;
    }
  }

  switchOrganization(orgId: string) {
    return this.http.post('/bff/org/switch', { orgId })
      .pipe(tap(() => this.refreshUser()));
  }

  requestDataExport() {
    return this.http.get('/bff/user/data-export');
  }

  requestAccountDeletion() {
    return this.http.post('/bff/user/request-deletion', {});
  }
}
```

### Auth Guard

```typescript
// src/app/core/auth/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return toObservable(authService.isLoading).pipe(
    filter(loading => !loading),
    take(1),
    map(() => {
      if (authService.isAuthenticated()) {
        return true;
      }
      authService.login(state.url);
      return false;
    })
  );
};
```

### Auth Interceptor

```typescript
// src/app/core/auth/auth.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Credentials included automatically for same-site cookies
  const authReq = req.clone({ withCredentials: true });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.login(window.location.pathname);
      }
      return throwError(() => error);
    })
  );
};
```

### PWA Update Service

```typescript
// src/app/core/services/update.service.ts
import { Injectable, inject } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class UpdateService {
  private swUpdate = inject(SwUpdate);
  private messageService = inject(MessageService);

  initialize() {
    if (!this.swUpdate.isEnabled) {
      console.log('Service Worker not enabled');
      return;
    }

    // Listen for new versions
    this.swUpdate.versionUpdates
      .pipe(filter((evt): evt is VersionReadyEvent => evt.type === 'VERSION_READY'))
      .subscribe(() => {
        this.messageService.add({
          severity: 'info',
          summary: 'Update Available',
          detail: 'A new version is available. Click to update.',
          sticky: true,
          data: { action: 'update' }
        });
      });

    // Check for updates periodically (every 30 minutes)
    setInterval(() => {
      this.swUpdate.checkForUpdate();
    }, 30 * 60 * 1000);
  }

  applyUpdate() {
    this.swUpdate.activateUpdate().then(() => {
      window.location.reload();
    });
  }
}
```

### App Configuration

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideServiceWorker } from '@angular/service-worker';
import { MessageService } from 'primeng/api';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    }),
    MessageService
  ]
};
```

---

## 8. Docker Compose Configuration

### docker-compose.landlord.yml

```yaml
services:
  landlord-bff:
    build:
      context: ../../src/landlord/portal/bff/ProperTea.Landlord.Bff
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - Auth__Authority=https://auth.propertea.localhost
      - Auth__ClientId=landlord-portal-bff
      - ConnectionStrings__Redis=redis:6379
    # CRITICAL: Routes auth.propertea.localhost through Traefik
    extra_hosts:
      - "auth.propertea.localhost:host-gateway"
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.landlord-bff.rule=Host(`landlord.propertea.localhost`) && (PathPrefix(`/bff`) || PathPrefix(`/api`))"
      - "traefik.http.routers.landlord-bff.tls=true"
      - "traefik.http.routers.landlord-bff.entrypoints=websecure"
      - "traefik.http.services.landlord-bff.loadbalancer.server.port=8080"
    networks:
      - propertea-network
    depends_on:
      - redis

  landlord-web:
    build:
      context: ../../src/landlord/portal/web/propertea-landlord-portal
      dockerfile: Dockerfile
      target: dev
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.landlord-web.rule=Host(`landlord.propertea.localhost`)"
      - "traefik.http.routers.landlord-web.tls=true"
      - "traefik.http.routers.landlord-web.entrypoints=websecure"
      - "traefik.http.services.landlord-web.loadbalancer.server.port=4200"
    networks:
      - propertea-network
    volumes:
      - ../../src/landlord/portal/web/propertea-landlord-portal:/app
      - /app/node_modules

networks:
  propertea-network:
    external: true
```

### Required Infrastructure (docker-compose.infra.yml additions)

```yaml
services:
  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    networks:
      - propertea-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  redis-data:
```

### Host File Entries (Development)

```
# /etc/hosts (Linux/Mac) or C:\Windows\System32\drivers\etc\hosts (Windows)
127.0.0.1 auth.propertea.localhost
127.0.0.1 landlord.propertea.localhost
127.0.0.1 propertea.localhost
```

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
