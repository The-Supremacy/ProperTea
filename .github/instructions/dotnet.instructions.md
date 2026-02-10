---
applyTo: "**/*.cs"
---

# .NET Service Development

Read `/docs/architecture.md` for full system context before making changes.

## Vertical Slice Architecture
Organize code by feature, not layer. Path: `Features/{FeatureName}/`.
Never create generic "Services", "Managers", or "Repositories" folders.
Reference: `ProperTea.Company/Features/Companies/` for complete example.

## Structure
- 'docs/dev/backend-feature-structure.md' defines the directory layout and conventions for features.

## Wolverine (Messaging & CQRS)
- Handlers implement `IWolverineHandler`.
- Command/query DTOs are `record` types defined in the same file as the handler.
- Wolverine manages transactions via `AutoApplyTransactions()`. Do NOT call `SaveChangesAsync` unless strictly required for immediate read-after-write (e.g., returning a new entity ID).
- Publish integration events via `IMessageBus.PublishAsync`.
- Endpoints use `[WolverinePost]`, `[WolverineGet]`, etc.
- Multi-tenancy: use `bus.InvokeForTenantAsync(tenantId, command)` with the org ID from `IOrganizationIdProvider`.

## Marten (Persistence & Event Sourcing)
- Inject `IDocumentSession` in handlers.
- All documents are multi-tenanted (`AllDocumentsAreMultiTenanted`).
- New streams: `session.Events.StartStream<TAggregate>(id, events)`.
- Existing streams: `session.Events.Append(id, events)`.
- Rehydrate: `session.Events.AggregateStreamAsync<TAggregate>(id)`.

## Multi-Tenancy Implementation
- ZITADEL org ID = Marten `TenantId` directly (no mapping, ADR 0010)
- Extract tenant via `IOrganizationIdProvider.GetOrganizationId()`
- Dispatch commands: `bus.InvokeForTenantAsync(tenantId, command)`
- Aggregates must implement `ITenanted` for automatic tenant scoping
- Marten auto-scopes all queries/streams to current tenant
- Read `/docs/dev/multi-tenancy-flow.md` for complete flow.

## Aggregate Pattern (Decider)
- Aggregates implement `IRevisioned` (and `ITenanted` when multi-tenant).
- Static factory for creation: `public static Created Create(...)` returns an event.
- Instance methods for mutations: `public Deleted Delete(...)` returns an event.
- Domain methods validate, then return immutable event records. They never mutate state directly.
- `Apply(EventType e)` methods inside the aggregate mutate state from events.
- Events are immutable `record` types in a separate static class (e.g., `CompanyEvents`).
- Import events via `using static`.

## Error Handling
- Error codes: `SCREAMING_SNAKE_CASE` constants in a static `{Feature}ErrorCodes` class.
- Use typed exceptions: `NotFoundException`, `BusinessViolationException`, `ConflictException`.
- Suppress CA1707 for error code classes with `#pragma warning disable CA1707`.

## Integration Events
- Mark with `[MessageIdentity("entity.action.v1")]`.
- Naming: `{entity}.{action}.v{version}`.
- Contracts (interfaces) live in `ProperTea.Contracts`. Implementations live in the publishing service.
- Update `/docs/event-catalog.md` when adding new events.

## Style
- Use `_ =` discard for suppressing fluent API return warnings.
- No XML doc comments unless describing something genuinely non-obvious.
