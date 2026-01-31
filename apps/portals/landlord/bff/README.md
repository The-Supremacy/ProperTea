# ProperTea.Landlord.Bff

**Responsibility**: Backend for Frontend (BFF) that secures the Landlord Portal and forwards authenticated requests to backend services.

## Overview
The Landlord BFF implements the BFF pattern using typed HTTP clients and delegating handlers. It handles OIDC authentication with ZITADEL, manages user sessions, and enriches downstream requests with user/organization context. **This BFF contains NO business logic** - it's a pure pass-through/mapper layer.

## Technical Stack
- **.NET 10**: Target framework
- **Typed HTTP Clients**: Strongly-typed service clients with DelegatingHandlers
- **ZITADEL**: External IdP (OIDC Code Flow)
- **JWT Authentication**: Bearer token forwarding to backend services

## Folder Structure

```
ProperTea.Landlord.Bff/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Auth/                        # Authentication utilities
│   ├── UserAccessTokenHandler.cs
│   └── OrganizationHeaderHandler.cs
├── Config/                      # Configuration
│   ├── AuthenticationConfig.cs
│   ├── InfrastructureConfig.cs
│   └── OpenApiConfig.cs
├── Organizations/               # Organization feature
│   ├── OrganizationClient.cs
│   ├── OrganizationDtos.cs
│   └── OrganizationEndpoints.cs
├── Users/                       # User feature
│   ├── UserClient.cs
│   ├── UserDtos.cs
│   └── UserEndpoints.cs
└── Companies/                   # Company feature
    ├── CompanyClient.cs
    ├── CompanyDtos.cs
    └── CompanyEndpoints.cs
```

## Design Principles

### BFF Pattern
- **Pure pass-through/mapper** - NO business logic
- Tailored API for specific frontend needs
- Aggregates multiple backend calls when necessary

### Responsibilities
- ✅ Handle OIDC authentication flow
- ✅ Manage user sessions
- ✅ Forward authenticated requests to services
- ✅ Map backend DTOs to frontend-friendly contracts
- ✅ Aggregate multiple backend service calls
- ❌ NO business logic or authorization checks (handled by services)
- ❌ NO direct database access
- ❌ NO event publishing

### Service Clients
Current clients configured:
- **OrganizationClient**: Organization registration and management
- **UserClient**: User profiles and preferences
- **CompanyClient**: Company CRUD operations

Each client uses:
- `UserAccessTokenHandler` - Attaches JWT bearer token
- `OrganizationHeaderHandler` - Attaches `X-Organization-Id` header

## Configuration

### Required Environment Variables
```bash
# OIDC Configuration
OIDC__Authority=<ZITADEL_URL>
OIDC__ClientId=<BFF_CLIENT_ID>
OIDC__ClientSecret=<BFF_CLIENT_SECRET>

# Service URLs (from Aspire)
services__organization__http__0=http://localhost:5001
services__user__http__0=http://localhost:5002
services__company__http__0=http://localhost:5003
```

## Development

### Running Locally
```bash
# From solution root with Aspire (recommended)
dotnet run --project orchestration/ProperTea.AppHost

# Standalone (requires manual service configuration)
cd apps/portals/landlord/bff
dotnet run
```

### Adding a New Service Client

1. Create feature folder: `{Feature}/`
2. Add client interface and implementation: `{Feature}Client.cs`
3. Add DTOs: `{Feature}Dtos.cs`
4. Add endpoints: `{Feature}Endpoints.cs`
5. Register client in `Program.cs` with `UserAccessTokenHandler` and `OrganizationHeaderHandler`

## Security Considerations

### Authentication Flow
1. Frontend receives JWT token from ZITADEL
2. Frontend includes token in requests to BFF
3. BFF validates token signature and claims
4. BFF forwards token to backend services
5. Services validate token independently

### Token Forwarding
- **UserAccessTokenHandler**: Extracts token from Authorization header, forwards to services
- **OrganizationHeaderHandler**: Extracts org_id from token claims, adds as X-Organization-Id header

### Defense in Depth
- BFF validates JWT signature
- Services validate JWT signature independently
- Services enforce organization-level data isolation via Marten tenancy
- Services check OpenFGA for resource-level permissions (future)

## DTOs
## DTOs
- Keep Request/Response DTOs in `{Feature}Dtos.cs` near endpoints
- Use `record` types for immutability
- `MapFrom` methods in Response DTOs for mapping backend models
- Never expose internal service models directly to frontend

## Best Practices
- **Minimal endpoints** - Only what frontend actually needs
- **Clear naming** - Follow REST conventions
- **Async all the way** - All service communication is async
- **Error handling** - Return appropriate HTTP status codes
- **Validation** - Basic request validation only (complex validation in services)

## Related Services
- **Organization Service**: Provides organization management
- **User Service**: Provides user profiles and preferences
- **Company Service**: Provides company management
- **Landlord Portal**: Angular SPA consuming this BFF

## Architecture Decisions
- [0003-headless-onboarding.md](../../../../docs/decisions/0003-headless-onboarding.md) - BFF role in registration
- [0008-authorization-hybrid-strategy.md](../../../../docs/decisions/0008-authorization-hybrid-strategy.md) - BFF pass-through pattern
