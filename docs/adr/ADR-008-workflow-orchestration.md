# ADR-008: Workflow Orchestration with Sagas

## Status
Accepted

## Context
Complex multi-service operations (registration, onboarding, approvals) require reliability and compensations. We want a simple, self-hosted approach without commercial dependencies.

## Decision
- Implement a custom saga orchestrator using PostgreSQL for durable state.
- Persist: SagaInstances, SagaSteps, StepExecutions, Compensations with idempotency keys.
- Retries: up to 3 with exponential backoff and jitter (base 200ms, cap 30s).
- Compensation: automatic rollback defined per step; short-circuit on validation failures.
- Dashboard: lightweight, read-only dashboard (tiny React page) served by the Workflow service.

## Consequences

Positive:
- Full control, no external orchestration dependency
- Durable, queryable history with Postgres
- Educational value and transparency

Negative:
- More code to write and maintain
- Fewer out-of-the-box features than dedicated orchestrators

Mitigations:
- Clear module boundaries and interfaces
- Good metrics (success/failure/age) and alerting
