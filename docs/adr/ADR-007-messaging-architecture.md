# ADR-007: Event-Driven Messaging Architecture

## Status
Accepted

## Context
We need asynchronous communication for decoupling and eventual consistency. Locally we avoid cloud dependencies; production will run on Azure.

## Decision
- Local broker: Kafka. Production: Azure Service Bus. Application code uses a thin IPublisher/IConsumer interface with pluggable implementations.
- Events formatted as CloudEvents (JSON).
- Use outbox pattern per service with a background dispatcher; at-least-once delivery and idempotent consumers.
- Retries with exponential backoff; DLQ (dead-letter topic/queue) for poison messages.
- Topic naming: domain.service.event.v1; organization context in payload, not topic name.

## Consequences

Positive:
- Decoupled services with reliable event delivery
- Easy local setup with Kafka; smooth switch to ASB later
- Standard CloudEvents format simplifies tooling and evolution

Negative:
- Additional moving parts (dispatcher, DLQ handling)
- Idempotency requirements for consumers

Mitigations:
- Provide base libraries for outbox and consumer idempotency
- Document patterns and include examples/tests
