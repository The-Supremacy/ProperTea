# Observability

## Goals
- Full visibility into requests, dependencies, and system health
- Local parity with production via OpenTelemetry
- Actionable dashboards and alerts

## Tracing
- OpenTelemetry for distributed tracing across gateway and services
- Propagate trace context via W3C TraceContext headers
- Local exporter: Jaeger. Production: Azure Monitor/App Insights
- Key spans: gateway routing, auth/permission fetch, DB calls, external calls

## Metrics
- Standard ASP.NET metrics + custom domain metrics
- Exporter: Prometheus (local), Azure Monitor (prod)
- SLIs:
  - Gateway p95 latency per route
  - Error rate per service
  - Auth failures and rate limit hits
  - Outbox dispatcher lag
  - Saga success/failure rates

## Logging
- Structured JSON logs with context: traceId, userId, orgId, route
- Local aggregation in Loki; dashboards in Grafana
- Log levels tuned to avoid noise; error logs always include correlation

## Sampling and Retention
- Local traces: 100% sampling
- Load testing: 10–20% sampling
- Production: 5–10% traces, 100% metrics/logs
- Local retention: 7 days

## Health and Readiness
- /health/live for container health
- /health/ready for downstream readiness (DB, cache, broker)
- /metrics for Prometheus scraping
- Gateway may aggregate downstream health summaries

## Dashboards
Suggested Grafana dashboards:
- Gateway overview: traffic, errors, latency, rate-limits
- Service drill-down: latencies, error codes, dependency timing
- Messaging: consumer lag, retries, DLQ counts
- Database: connections, CPU, slow queries (via exporters)

## Alerts (examples)
- Error rate spike > 5% for 5 minutes
- Gateway p95 latency > 1s for 10 minutes
- Outbox lag > 60s
- DLQ growth > 10 messages/min
- Saga failures > baseline + 3σ
