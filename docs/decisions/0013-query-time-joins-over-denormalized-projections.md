# ADR 0013: Query-Time Joins Over Denormalized Projections

**Status**: Accepted
**Date**: 2026-02-11
**Deciders**: Team

## Context
List views (Properties, Units) need to display names from related aggregates: a Property list shows the Company name, a Unit list shows both Property name and Company name.

We evaluated two approaches for resolving these cross-aggregate names:

### Option A: Denormalized Async Projections
Maintain separate projection documents (`PropertyListItemProjection`, `UnitListItemProjection`) that store denormalized copies of related names. Cross-aggregate Wolverine handlers listen for Company/Property events and bulk-update all affected projection rows.

### Option B: Query-Time Join via Inline Aggregates
Keep only the inline snapshot aggregates (`PropertyAggregate`, `UnitAggregate`). At query time, resolve related names by looking up `CompanyReference` and `PropertyAggregate` documents in-memory after the paginated fetch.

### The Fan-Out Problem
Option A creates a write amplification problem. When a company name changes:
- Every `PropertyListItemProjection` for that company must be updated
- Every `UnitListItemProjection` for that company must be updated
- A company with 500 properties and 5,000 units triggers 5,500 document writes for a single name change
- This scales linearly with tenant size and gets worse over time

The inline aggregates are already materialized synchronously on every event append. They are always current and indexed. The "join" is just 1-2 additional queries against documents already in the same PostgreSQL database, scoped to the page size (typically 25-50 IDs).

## Decision
We adopt **Option B: Query-Time Joins** using inline snapshot aggregates and `CompanyReference` documents for cross-aggregate name resolution.

List handlers query the aggregate, paginate, then resolve related names via a batched `IN` query against the reference documents. This happens within a single Marten session (single database connection, same transaction).

## Consequences

### Positive
* **Zero write amplification**: Company/Property renames are O(1), not O(n)
* **Always consistent**: Inline aggregates are updated synchronously - no eventual consistency lag
* **No extra infrastructure**: No async daemon, no projection rebuild pipeline, no cross-aggregate handlers
* **Simpler code**: ~200 fewer lines; no projection documents, no 9 synchronization handlers
* **Schema evolution is trivial**: Adding a new field to the list response is a handler change, not a projection rebuild

### Negative
* **Slightly more queries per list request**: 2-3 queries instead of 1 (aggregate + reference lookup)
* **No server-side sort on denormalized columns**: Cannot `ORDER BY CompanyName` at the database level since it lives in a different document. Client-side sort is acceptable for paginated list views
* **Name resolution is per-page**: Each page request resolves names independently (no caching across pages)

### Risks / Mitigation
* **Performance at scale** -> The additional queries are batched `IN` lookups against indexed documents, scoped to page size. At 50 items per page, the `IN` clause contains at most 50 IDs. Marten's lightweight sessions make this efficient. If this becomes a bottleneck, we can introduce a caching layer or reconsider projections for specific high-traffic views
* **Stale CompanyReference** -> CompanyReference is updated via Wolverine durable messaging (at-least-once delivery). In the rare case of a stale reference, the name shown is seconds behind - acceptable for a list view

## Event Replay Strategy
For future reference, Marten provides built-in replay capabilities:
- `store.Advanced.RebuildProjectionAsync<T>()` for full async projection rebuilds
- The async daemon tracks high-water marks per projection for automatic catch-up
- Wolverine's durable outbox ensures at-least-once delivery for integration events over RabbitMQ
- Kafka is not needed at current scale; RabbitMQ + Wolverine durability is sufficient
