# Workflows and Sagas

## When to Use
- Multi-service operations needing consistency (registration, onboarding)
- Long-running flows with compensations
- Operations requiring retries and monitoring

## Orchestration Model
- Central Workflow service acts as orchestrator
- Durable state in Postgres:
  - SagaInstances (correlation, status)
  - SagaSteps (definition and order)
  - StepExecutions (attempts, outcomes, idempotency keys)
  - Compensations (reverse actions)
- Retry policy: up to 3, exponential backoff with jitter (200ms–30s)

## Idempotency and Dedupe
- Each step is idempotent
- Record `(sagaId, stepId, attemptNo, status)` to avoid duplicate effects
- Use idempotency keys for external side effects

## Dashboard
- Tiny React page served by Workflow service
- Read-only: list instances, view steps, filter by status/date
- Drill-down into failures and compensation paths

## Failure Modes
- Service unavailable: retry per policy, then compensate
- Validation failure: no retry; mark step failed and compensate
- Timeout: treat as failure and compensate

## Security and Audit
- Internal token from gateway for Workflow service calls
- Audit every step transition, retries, and compensations
- Emit “workflow.*” audit events
