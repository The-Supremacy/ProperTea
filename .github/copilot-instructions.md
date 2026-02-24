# ProperTea - AI Instructions

This is a .NET Aspire monorepo for a multi-tenant Real Estate ERP.

## Project Layout
- `/apps/services/` - Backend microservices (C#, Wolverine + Marten)
- `/apps/portals/landlord/bff/` - BFF gateway (pass-through only, no business logic)
- `/apps/portals/landlord/web/` - Angular 21 SPA (Tailwind, signals, zoneless)
- `/shared/ProperTea.Contracts/` - Source of truth for cross-service integration models
- `/orchestration/` - .NET Aspire AppHost

## Running the Project

**Start everything** (all services, databases, ZITADEL, RabbitMQ):
```bash
dotnet run --project orchestration/ProperTea.AppHost
```
Access Aspire Dashboard at `https://localhost:17285` for logs, metrics, and traces.

**Frontend** runs at `http://localhost:4200`, backend services are on dynamic ports (check Aspire Dashboard).

## Documentation System

Read docs **before** making changes:
- `/docs/architecture.md` - System context and service boundaries (READ FIRST)
- `/docs/domain.md` - Ubiquitous language (exact terms to use in code)
- `/docs/event-catalog.md` - Cross-service integration events
- `/docs/dev/` - Development patterns and conventions
- `/docs/decisions/` - ADRs explaining the "why" behind design choices

Example: ADR 0010 explains why we use ZITADEL org ID directly as `TenantId` (no mapping layer).

## AI Agent System

This repo has a structured AI instruction system:

1. **Context-specific instructions** in `.github/instructions/`:
   - `dotnet.instructions.md` - Applied to all `**/*.cs` files
   - `angular.instructions.md` - Applied to `**/landlord-portal/**`
   - `bff.instructions.md` - Applied to `**/Bff/**`
   - `contracts.instructions.md` - Applied to `**/ProperTea.Contracts/**`

2. **Complex task skills** in `.github/skills/`:
   - `new-backend-feature` - Scaffold aggregate, events, handlers, endpoints
   - `new-angular-feature` - Scaffold feature module, routes, components
   - `new-integration-event` - Wire cross-service events end-to-end

When asked to create features, **read the relevant skill file** from `.github/skills/{skill-name}/SKILL.md` for step-by-step patterns.

NEVER EXECUTE GIT COMMANDS OR MAKE CHANGES WITHOUT EXPLICIT INSTRUCTION TO DO SO. Always ask for confirmation before making any changes to the repository.

## Code Quality

- `Directory.Build.props` enforces `TreatWarningsAsErrors=true` project-wide
- Use `LangVersion=preview` for latest C# features
- No XML doc comments unless genuinely non-obvious
- Use proper markdown. No emojis.
