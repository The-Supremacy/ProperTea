# Data and Storage
# Data and Storage Architecture

## Overview

ProperTea uses a polyglot persistence approach with PostgreSQL as the primary database, Redis for caching and sessions, and specialized storage for different data types.

## Primary Database: PostgreSQL

### Database Strategy
- **Single Database**: All services share one PostgreSQL instance with service-specific schemas
- **Multitenancy**: Shared-table approach with OrganizationId discriminator column
- **Migrations**: Per-service EF Core migrations with centralized execution
- **Connection Management**: Connection pooling with proper timeout and retry policies

### Schema Organization
## Databases
- PostgreSQL as primary relational store
- Shared-table multitenancy with OrganizationId discriminator
- EF Core code-first migrations per service
- Avoid vendor-specific features for Azure SQL portability

## Caching
- Redis for:
  - Permission model caching (optional)
  - Short-lived data and request-level caches
  - Data Protection keys for ASP.NET (shared across replicas)

## Object Storage
- Local: Azurite
- Production: Azure Blob Storage
- Use for file uploads and document storage

## Search
- Local: OpenSearch for property listing search
- Production: Azure Cognitive Search (future)
- Index via Search Indexer consuming property events

## Backups and Retention
- Nightly Postgres dump locally
- Production: use managed backups and configured retention
- Logs/metrics retention short locally to preserve disk

## GDPR and PII
- PII encrypted at rest where appropriate
- Anonymization on deletion requests:
  - Replace PII with irreversible placeholders
  - Keep salted hash of former email for dedupe prevention
- Maintain referential integrity for non-erasable business records

## Migrations Strategy
- Dev/QA: auto-apply migrations on startup (feature-flagged)
- Prod: run dedicated migrator job
- Track migration versions per service
