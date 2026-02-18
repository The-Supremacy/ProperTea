# ADR 0015: Extract Building as a Separate Aggregate

**Status**: Accepted
**Date**: 2026-02-14
**Deciders**: Team

## Context
Building is currently modeled as a child entity within the Property aggregate. Events like `BuildingAdded`, `BuildingUpdated`, and `BuildingRemoved` are stored in the Property event stream. All Building command handlers load the entire `PropertyAggregate` to access the `Buildings` list.

This creates several problems:

1. **Write contention**: Every Building mutation bumps the Property aggregate version. Editing a Building competes with Property edits for optimistic concurrency.
2. **Unnecessary loading**: Five Building handlers load the full Property state (with all events) just to access child entities.
3. **Audit log pollution**: Building changes appear in the Property history. A Building code rename is unrelated to the Property itself.
4. **Independent query needs**: We need a standalone list-view for Buildings with filtering, sorting, and pagination. Currently this requires either a Marten projection or loading the Property aggregate and filtering in memory.
5. **Future growth**: Building will carry accounting/coding attributes (cost centers, GL accounts, classification codes) and its own field-level events. This will compound all the above problems.

ADR 0001 established the precedent: Unit was separated from Property for identical reasons. Building has the same relationship shape (many per Property, independent lifecycle) and the same argument applies.

## Decision
Extract Building into its own Aggregate Root within the Property Service.

1. `BuildingAggregate` is a new Aggregate Root with its own Marten event stream.
2. Building holds a `PropertyId` reference to its parent, matching Unit's pattern.
3. Building code uniqueness is enforced at handler level via query-time check (same as Property code uniqueness within a Company).
4. Existing Property endpoints for Buildings (`/properties/{propertyId}/buildings/*`) remain at the same URLs. The route structure reflects the domain hierarchy, not the aggregate boundary.
5. `BuildingCount` on Property list views is resolved via query-time join (ADR 0013 pattern), not denormalized on the Property aggregate.
6. Building events are removed from the Property event stream. Existing historical events (`property.building-added.v1`, etc.) remain in the store but are ignored by the Property aggregate's inline snapshot.

## Consequences

### Positive
- **No write contention**: Building and Property edits are independent event streams with separate versioning
- **Lighter loads**: Building handlers load only the Building aggregate, not the entire Property with all its events
- **Clean audit log**: Property history shows only Property changes. Building gets its own audit log
- **Independent queries**: `BuildingAggregate` inline snapshots are directly queryable with Marten (pagination, filtering, sorting) without custom projections
- **Matches established patterns**: Identical to Property-Unit separation (ADR 0001) and Company aggregate structure
- **Future-proof**: Accounting attributes, field-level events, and Building-specific business rules are cleanly scoped

### Negative
- **Eventual consistency for cascading deletes**: Deleting a Property requires a cross-aggregate process to remove child Buildings (same as Property-Unit today)
- **Building code uniqueness is query-time**: No transactional guarantee (same trade-off as Property code within Company; acceptable given the low collision probability)
- **Migration effort**: Existing Building events in Property streams are orphaned. Building-related code must be extracted from ~20 files across backend, BFF, and frontend

### Risks / Mitigation
- **Existing event data** -> Historical `property.building-*` events remain in the Property stream but are harmless. The Property aggregate no longer has `Apply()` methods for them, and Marten's inline snapshot ignores unknown event types. Building audit log starts fresh from the new stream; historical Building changes are still visible in Property's audit log for events that occurred before the extraction.
- **Unit validation** -> `CreateUnitHandler` currently validates `BuildingId` against `property.Buildings`. After extraction, it queries `BuildingAggregate` directly, which is actually simpler (no need to load the entire Property).
