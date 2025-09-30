# Search Indexer Service

## Purpose
Index property listings into OpenSearch for search and discovery.

## Responsibilities
- Consume listing events from Kafka (or ASB later)
- Transform and upsert documents into OpenSearch
- Provide simple search API for learning purposes

## Data Flow
- Outbox → Dispatcher → Topic → Search Indexer → OpenSearch

## Indexing Cadence
- Dispatcher runs every 1s locally; batch writes to OpenSearch
- Accept eventual consistency

## API (optional)
- `GET /search?q=&filters=` (for learning; FE may call directly or via gateway)

## Observability
- Metrics: indexing throughput, failures
- Traces: consume → transform → index
- Logs: per-batch results and DLQ events
