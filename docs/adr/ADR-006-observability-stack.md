# ADR-006: Observability and Monitoring

## Status
Accepted

## Context
We need full local observability with parity to production, using standards-based instrumentation that can map to Azure services later without code changes.

## Decision
- Use OpenTelemetry SDK for traces, metrics, and logs across all services.
- Local: OTel Collector → Jaeger (traces), Prometheus (metrics), Grafana (dashboards), Loki (logs).
- Production: OTel Collector → Azure Monitor/Application Insights exporters.
- Sampling: local 100%; load testing 10–20%; production 5–10% traces, 100% metrics/logs.
- Standardize health endpoints (/health/live, /health/ready) and metrics (/metrics).

## Consequences

Positive:
- Consistent, vendor-neutral instrumentation
- Full local stack for learning and troubleshooting
- Easy mapping to Azure with config-only changes

Negative:
- Local stack uses resources
- Requires baseline OTel knowledge

Mitigations:
- Ship sane defaults, dashboards, and runbooks
- Keep retention short locally (7 days) to limit disk usage
