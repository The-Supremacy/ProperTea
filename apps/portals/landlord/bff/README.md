# BFF Structure

## Folder Structure Template

```
ProperTea.{Portal}.Bff/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Auth/                        # Authentication utilities
│   └── ...
├── Config/                      # Configuration
│   ├── AuthenticationConfig.cs
│   ├── InfrastructureConfig.cs
│   └── OpenApiConfig.cs
└── {Feature}/                   # One folder per backend feature
    ├── {Feature}Client.cs       # HTTP client to backend service
    ├── {Feature}Dtos.cs         # Request/Response DTOs
    └── {Feature}Endpoints.cs    # API endpoints
```

## Design Principles

### BFF Pattern
- **Pure pass-through/mapper** - NO business logic
- Tailored API for specific frontend needs
- Aggregates multiple backend calls when necessary

### Responsibilities
- ✅ Map backend DTOs to frontend-friendly contracts
- ✅ Aggregate multiple backend service calls
- ✅ Enforce authentication/authorization
- ❌ NO business logic
- ❌ NO direct database access
- ❌ NO event publishing

### Communication
- Uses typed HTTP clients to communicate with backend services
- Service discovery via .NET Aspire
- All backend communication is async

### DTOs
- Keep Request/Response DTOs near endpoints
- `MapFrom` methods in Response DTOs for mapping
- Never map directly from Request DTO to entity

### Authentication
- Cookie-based authentication
- JWT bearer token validation via Zitadel
- Frontend sends cookies, BFF validates and forwards auth context

### Endpoints
- Minimal endpoints - only what frontend needs
- Clear naming following REST conventions
- Use `[AsParameters]` for query parameters
- Anonymous endpoints explicitly marked with `AllowAnonymous()`
