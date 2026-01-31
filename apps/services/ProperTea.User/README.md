# ProperTea.User

**Responsibility**: Manages user profiles, preferences, and "Last Seen" tracking for authenticated users within organizations.

## Overview
This service maintains user-specific data including profile information, UI preferences, and activity tracking. It reacts to organization registration events to create initial user profiles and provides user context to the frontend applications.

## Technical Stack
- **.NET 10**: Target framework
- **Marten**: Event sourcing for `UserProfileAggregate`, document storage for projections and preferences
- **Wolverine**: CQRS handlers and messaging infrastructure
- **RabbitMQ**: Integration event consumption
- **JWT Authentication**: Extracts user identity from ZITADEL tokens

## Key Concepts

### Aggregates
- **UserProfileAggregate**: Event-sourced user profile (name, email, status)
- **UserPreferences**: Document model for UI preferences (theme, language, notifications)

### Multi-Tenancy
User data is **multi-tenanted** (`ITenanted`). Users exist within organization context and can only access their own profile within their tenant scope.

### User Identity
Uses ZITADEL user ID (from `sub` claim) directly as the user identifier. No internal user ID mapping layer.

## Configuration

### Required Environment Variables
```bash
OIDC__Authority=<ZITADEL_URL>
OIDC__Issuer=<ZITADEL_ISSUER>
OIDC__Audience=<API_AUDIENCE>
ConnectionStrings__ProperTeaDb=<PostgreSQL_Connection>
RabbitMQ__Host=<RabbitMQ_Host>
```

## Development

### Running Locally
From solution root with Aspire:
```bash
dotnet run --project orchestration/ProperTea.AppHost
```

## Architecture Decisions
- [0009-user-identity-strategy.md](../../../docs/decisions/0009-user-identity-strategy.md) - ZITADEL user ID usage
- [0007-organization-multi-tenancy.md](../../../docs/decisions/0007-organization-multi-tenancy.md) - Multi-tenancy implementation

## Related Services
- **Organization Service**: Publishes registration events that trigger profile creation
- **All BFFs**: Consume user profile and preferences for session enrichment
