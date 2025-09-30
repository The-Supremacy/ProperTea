# Local Developer Experience

## Goals
- Full local stack with production parity (HTTPS, observability)
- Simple onboarding with Makefile

## Prerequisites
- Docker + Compose, Git, .NET 9 SDK, Node.js 18+, Make

## Quick Start (typical)
- make bootstrap
- make certs
- make infra-up
- make services-up
- make logs

## Make Targets (examples)
- bootstrap, build, rebuild, up, down, restart
- infra-up, services-up
- test, lint, format, watch
- logs, logs-follow, ps, health-check
- open (open key UIs), grafana, jaeger

## Local HTTPS
- Traefik + mkcert; hosts like api.local.test, identity.local.test
- Add host entries; generate certs with mkcert

## Observability UIs
- Grafana, Prometheus, Jaeger, Loki (links depend on compose)

## Troubleshooting
- Check container logs (docker compose logs -f SERVICE)
- Verify DB connectivity; reset DB if needed
- Regenerate certs if browser shows warnings
