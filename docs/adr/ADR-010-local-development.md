# ADR-010: Local Development Environment

## Status
Accepted

## Context
We want full-stack local development with production parity, without any external cloud dependencies.

## Decision
- Use Docker Compose to run all services and infrastructure locally (Postgres, Redis, Kafka, OpenSearch, OTel stack).
- Use Traefik + mkcert for local TLS and hostnames (*.local.test).
- Provide a Makefile for bootstrap, build, test, run, logs, and health checks.
- Prefer HTTPs for all local endpoints; keep short retention for observability data.

## Consequences

Positive:
- Consistent and reproducible local environment
- Easy onboarding with Makefile targets
- Parity with production security constraints (TLS, OIDC flows)

Negative:
- Higher local resource usage
- Complexity of multi-service orchestration

Mitigations:
- Scale down services during development
- Offer scripts to start subsets of the stack
