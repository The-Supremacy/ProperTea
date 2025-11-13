# Migration Guide - Refactoring Existing Code

**Version:** 1.1.0  
**Last Updated:** October 30, 2025  
**Status:** Phase 1 Implementation Guide - Updated

---

## Table of Contents

1. [Overview](#overview)
2. [Identity Service Refactoring](#identity-service-refactoring)
3. [Landlord BFF Refactoring](#landlord-bff-refactoring)
4. [Migration Checklist](#migration-checklist)
5. [Testing After Migration](#testing-after-migration)

---

## Overview

This guide helps you refactor existing Identity Service and Landlord BFF code to align with the architecture documented
in `00-architecture-overview.md` and `01-authentication-authorization.md`.

### Current State

**What's Good:**

- ✅ Basic authentication flow works (login, JWT generation)
- ✅ Session management in BFF (Redis-backed)
- ✅ HTTP-only cookies for security
- ✅ Token reissuance logic
- ✅ Integration tests exist

**What Needs Refactoring:**

- ❌ No choreographed event publishing for user registration
- ❌ No separate worker projects
- ❌ No JWT enrichment with org/permissions
- ❌ Missing domain events and outbox pattern
- ❌ No integration with Contact, Organization, Permission services (they don't exist yet)

### Migration Strategy

**Phase 1a: Identity Service** (Week 1)

1. Create worker project
2. Add outbox pattern
3. Update registration endpoint to publish events
4. Keep existing endpoints working

**Phase 1b: Landlord BFF** (Week 4)

1. Add JWT enrichment middleware (on-demand)
2. Integrate with Permission Service
3. Update session structure

---

## Identity Service Refactoring

### Step 1: Simplify User Registration (Choreography Pattern)

**Decision:** User registration uses **choreographed events** - no saga needed.

**Why choreography, not saga:**

- ✅ Contact creation can fail independently without affecting user creation
- ✅ Contact is created later (deferred until user first accesses an org)
- ✅ No rollback needed - user can exist without a contact initially
- ✅ Each service reacts independently to events

**New Flow:**

1. User registers → Identity creates User → Publishes `UserCreated` event → Done
2. User logs in → BFF detects no Contact for current org → Redirects to onboarding
3. User fills profile → Contact Service creates PersonalProfile
4. Contact Service publishes `ContactCreated` event
5. Permission Service listens → Assigns default groups

**No orchestration needed.** Each service knows what to do when it receives an event.

**Implementation:**

Update the registration endpoint to publish an integration event:

```csharp
// services/Identity/ProperTea.Identity.Service/Endpoints/AuthEndpoints.cs
app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<ProperTeaUser> userManager,
    IIntegrationEventPublisher eventPublisher) =>
{
    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        return Results.BadRequest("Email and password required");

    var user = new ProperTeaUser
    {
        UserName = request.Email,
        Email = request.Email,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    // Publish event - other services react independently
    // NOTE: Contact creation is deferred until user first accesses an organization
    await eventPublisher.PublishAsync(new UserCreatedIntegrationEvent
    {
        UserId = user.Id,
        Email = user.Email,
        CreatedAt = DateTime.UtcNow
    });

    return Results.Ok(new { message = "Registration successful", userId = user.Id });
});

public record RegisterRequest(string Email, string Password);
```

**Define Integration Event:**

```csharp
// services/Identity/ProperTea.Identity.Service/IntegrationEvents/UserCreatedIntegrationEvent.cs
using ProperTea.ProperIntegrationEvents;

public class UserCreatedIntegrationEvent : IntegrationEventBase
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public override string EventType => "UserCreated";
    public override string Topic => "identity-events";
}
```

```

### Step 2: Create Worker Project (For Future Event Consumers)

Create a worker project for consuming integration events (e.g., for future GDPR saga):

```bash
cd services/Identity
dotnet new worker -n ProperTea.Identity.Worker
cd ProperTea.Identity.Worker
dotnet add reference ../ProperTea.Identity.Service/ProperTea.Identity.Service.csproj
dotnet add package ProperTea.ProperIntegrationEvents
dotnet add package ProperTea.ProperTelemetry
```

**Worker Program.cs:**

```csharp
// services/Identity/ProperTea.Identity.Worker/Program.cs
using ProperTea.Identity.Worker;
using ProperTea.ProperIntegrationEvents;
using ProperTea.ProperTelemetry;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Add telemetry
        services.AddProperTelemetry(hostContext.Configuration);
        
        // Add integration events
        services.AddProperIntegrationEvents()
            .UseKafka(hostContext.Configuration);
        
        // Add background workers (for future sagas)
        // services.AddHostedService<GDPRDeletionSagaOrchestrator>();
    })
    .Build();

await host.RunAsync();
```

### Step 3: Update Database Schema

**Remove saga table (if it exists):**

```bash
cd services/Identity/ProperTea.Identity.Service
dotnet ef migrations add RemoveUserRegistrationSaga
dotnet ef database update
```

**Keep user tables as-is:**

```sql
-- Users table (from ASP.NET Identity)
CREATE TABLE users (
    id UUID PRIMARY KEY,
    user_name VARCHAR(256) NOT NULL UNIQUE,
    email VARCHAR(256) NOT NULL,
    email_confirmed BOOLEAN DEFAULT FALSE,
    password_hash VARCHAR(500),
    -- ... other Identity fields
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);

-- External logins table
CREATE TABLE external_logins (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    provider VARCHAR(50) NOT NULL,
    provider_key VARCHAR(200) NOT NULL,
    provider_display_name VARCHAR(200),
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(provider, provider_key)
);

-- Outbox table (for reliable event publishing)
CREATE TABLE integration_event_outbox (
    id UUID PRIMARY KEY,
    event_type VARCHAR(200) NOT NULL,
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    processed_at TIMESTAMP NULL,
    retry_count INT DEFAULT 0
);
```
