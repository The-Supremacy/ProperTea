# ProperTea - AI Instructions

This is a .NET Aspire monorepo for a multi-tenant Real Estate ERP.

## Project Layout
- `/apps/services/` - Backend microservices (C#, Wolverine + Marten)
- `/apps/portals/landlord/bff/` - BFF gateway (pass-through only, no business logic)
- `/apps/portals/landlord/web/` - Angular 21 SPA (Tailwind, signals, zoneless)
- `/shared/ProperTea.Contracts/` - Source of truth for cross-service integration models
- `/orchestration/` - .NET Aspire AppHost

## Documentation
- Read `/docs/architecture.md` for system context and service boundaries.
- Read `/docs/domain.md` for ubiquitous language.
- Read `/docs/event-catalog.md` before adding or consuming integration events.
- Dev guides in `/docs/dev/`.
- Use proper markdown. No XML doc comments unless genuinely non-obvious. No emojis.
