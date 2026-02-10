---
applyTo: "**/ProperTea.Contracts/**"
---

# Shared Contracts

`ProperTea.Contracts` is the **source of truth** for all cross-service integration models.

## Rules
- Define **interfaces only**. Never concrete types.
- One file per bounded context: `Events/{Context}IntegrationEvents.cs`.
- Interface naming: `I{Entity}{Action}` (e.g., `ICompanyCreated`, `IOrganizationRegistered`).
- Properties are `{ get; }` only (read-only contract).
- Concrete implementations (`record` types with `[MessageIdentity]`) live in the publishing service, not here.

## When Adding a New Contract
1. Add the interface here.
2. Implement it in the publishing service.
3. Update `/docs/event-catalog.md` with the new event.
