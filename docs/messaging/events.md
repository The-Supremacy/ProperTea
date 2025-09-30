# Messaging and Events

## Overview
ProperTea uses asynchronous messaging for decoupling and eventual consistency. Locally we use Kafka; in production we can switch to Azure Service Bus via an interface without changing application code.

## Brokers
- Local: Kafka
- Production: Azure Service Bus (ASB)
- Abstraction: IPublisher/IConsumer with pluggable implementations

## Event Format
- CloudEvents (JSON), minimal required attributes:
  - id, type, source, specversion, time, datacontenttype, data
- Extensions:
  - tenant (organizationId), correlationId, causationId, version

## Topic Naming
- domain.service.event.v1
- Examples: property.listing.created.v1, companies.company.updated.v1

## Outbox Pattern
- Per-service outbox table stores events in same transaction as domain changes
- Background dispatcher publishes to broker
- On publish failure: retry with exponential backoff, then DLQ

## Consumer Idempotency
- Use event id + dedupe store to ensure at-least-once delivery is safe
- Side-effect operations must be idempotent (e.g., upsert semantics)

## Dead Letter Handling
- DLQ topic/queue per consumer group
- Include error metadata and last exception summary
- Manual or automated reprocessing after fixes

## Schema Governance
- No schema registry initially
- Prefer backward-compatible, additive changes
- Breaking change: new event type suffix (e.g., .v2)

## Security and Tenancy
- Include organizationId in event payload
- Validate org context before processing consumer side-effects
- Sign events if needed for inter-tenant boundaries (future)

## Monitoring
- Producer publish success/failure metrics
- Consumer lag, retry, and DLQ metrics
- Tracing spans for publish and consume with correlation
