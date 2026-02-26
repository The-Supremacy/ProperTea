# Prod Cluster — Pre-Launch Checklist

Settings that are intentionally simplified for local development and **must be changed** before prod is provisioned.

## Loki

| Setting | Local | Prod |
|---|---|---|
| `deploymentMode` | `SingleBinary` | `SimpleScalable` — separate write, read, backend components each with their own replica count |
| `loki.auth_enabled` | `false` | `true` — enforces per-tenant log isolation via `X-Scope-OrgID` header |
| `loki.replication_factor` | `1` | `3` — single replica means an in-flight write pod failure = log loss |
| `loki.storage.type` | `filesystem` (local PVC) | Object store (S3/GCS/Azure Blob) — local FS is not HA, not scalable, and not recoverable if the PVC node fails |
| `chunksCache.enabled` | `false` | `true` — memcached caches are significant at query volume |
| `resultsCache.enabled` | `false` | `true` |
| `lokiCanary.enabled` | `false` | `true` — DaemonSet that actively probes the full write→query path and exposes `loki_canary_dropped_entries_total`; this is how you get alerted before users notice logs are missing |
| `test.enabled` | `false` | Can remain `false` post-deploy; only useful during `helm test` CI validation |

## RabbitMQ

| Setting | Local | Prod |
|---|---|---|
| `auth.password` | `Password1!` hardcoded in `values.yaml` | Must come from an Infisical-managed ExternalSecret — do not commit credentials |
| `replicaCount` | `1` | `3` — quorum queues require an odd number of replicas ≥ 3 for majority acknowledgement |

## Keycloak

| Setting | Local | Prod |
|---|---|---|
| Replica count | `1` | `2+` with session affinity or distributed cache (Infinispan) |
| Database | Bundled / single CNPG instance | Dedicated CNPG cluster with 3 instances |

## CloudNativePG (all databases)

| Setting | Local | Prod |
|---|---|---|
| `instances` | `1` (no HA) | `3` — single instance has no standby; primary failure = downtime until manual recovery |
| Backup | Not configured | WAL archiving to object store + scheduled base backups |

## metrics-server

| Setting | Local | Prod |
|---|---|---|
| `--kubelet-insecure-tls` arg | Present (bypasses kubelet TLS validation) | Remove — AKS nodes have valid kubelet certs issued by the cluster CA |

## Longhorn

| Setting | Local | Prod |
|---|---|---|
| `defaultReplicaCount` | `2` (matches 2-node cluster) | `3` — requires 3 schedulable nodes; 2 replicas on prod means losing one node loses data redundancy |
| `storageOverProvisioningPercentage` | `200` (inflated to work around low-capacity local nodes) | Tune to `100` on properly sized prod nodes; 200 is artificially permissive |

## PVC Sizes

All PVC sizes were set to survive single-digit Gi local volumes. Size every volume against actual prod retention/throughput requirements before provisioning.

| Volume | Local | Notes |
|---|---|---|
| Loki | 10Gi | A single busy service can fill this in hours |
| Tempo | 5Gi | Trace retention will be very short at real traffic |
| VictoriaMetrics | 5Gi | 15-day retention + real scrape intervals will need 50–200Gi |
| RabbitMQ | 5Gi | Fine unless you have large message payloads or deep queues |
