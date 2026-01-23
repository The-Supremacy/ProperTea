# 0004: Contextual Authorization for Contractors

**Status**: Accepted
**Date**: 2026-01-22
**Deciders**: Development Team

## Context
Contractors need access to work orders owned by other tenants. Synchronizing these transient assignments to a permanent FGA store risks data drift and synchronization complexity.

## Decision
Implement **Contextual Tuples** in OpenFGA:
1. **Source of Truth**: The Work Order database holds the `ExecutorOrganizationId`.
2. **Just-in-Time Proof**: When performing an authorization check, the service fetches the assignment from the DB and passes it to OpenFGA as a temporary "Contextual Tuple".
3. **Check**: OpenFGA verifies the user is part of the Contractor Org AND that the Org is the current Executor.

## Consequences
### Positive
* No data drift between DB and FGA.
* Simplified assignment logic (pure SQL/Marten update).
### Negative
* Slight overhead in passing tuples with every check request.
