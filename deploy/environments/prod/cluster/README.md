# Prod Cluster — Pre-Launch Checklist

Settings that are intentionally simplified for local development and **must be changed** before prod is provisioned.

## Grafana

| Setting | Local | Prod |
|---|---|---|
| `grafana.ini.security.secret_key` | Hardcoded string in values.yaml | Must come from an ExternalSecret — key must be ≥32 chars, stable across restarts |
| `adminPassword` | `Password1!` hardcoded | ExternalSecret |

## VictoriaLogs

ProperTea uses VictoriaLogs instead of Loki. Same vendor as VictoriaMetrics, single-binary deployment, Loki-compatible push and query API (Alloy and Grafana require no structural changes — only endpoint URLs). Uses 5-10x less disk than Loki for equivalent log volume due to columnar compression.

| Setting | Local | Prod |
|---|---|---|
| `server.retentionPeriod` | `4w` | Set to match compliance/operational requirements — VictoriaLogs retention is time-based, not storage-based |
| `server.persistentVolume.size` | `3Gi` | Size against actual log volume — VictoriaLogs compresses far better than Loki, so a 20-50Gi PVC covers most prod workloads |
| `server.resources.limits.memory` | `256Mi` | Increase based on log ingestion rate; VictoriaLogs is very memory-efficient |

No object store required at any scale — VictoriaLogs stores everything in the PVC. This eliminates the main Loki prod complexity (S3/GCS/Azure Blob setup, IAM roles, lifecycle policies). The cost saving compared to Loki + object store is significant: no blob storage egress costs, no storage account, no per-operation charges.

## RabbitMQ

| Setting | Local | Prod |
|---|---|---|
| `auth.password` | `Password1!` hardcoded in `values.yaml` | Must come from an Infisical-managed ExternalSecret — do not commit credentials |
| `replicaCount` | `1` | `3` — quorum queues require an odd number of replicas ≥ 3 for majority acknowledgement |
| `extraConfiguration.default_queue_type` | Not set (classic queues) | `quorum` — quorum queues replicate to ⌊n/2⌋+1 brokers; cluster survives one broker loss without message loss |
| `affinity.podAntiAffinity` | Not set | `requiredDuringSchedulingIgnoredDuringExecution` on `kubernetes.io/hostname` — ensures each broker lands on a different node |

## Keycloak

| Setting | Local | Prod |
|---|---|---|
| Replica count | `1` | `2+` with session affinity or distributed cache (Infinispan) |
| Database | Bundled / single CNPG instance | Dedicated CNPG cluster with 3 instances |

## CloudNativePG (all databases)

| Setting | Local | Prod |
|---|---|---|
| `instances` | `2` (1 primary + 1 standby, fits 2 worker nodes) | `3` — adds a second standby; survives primary loss and retains one standby. Requires 3 schedulable nodes to satisfy `required` anti-affinity |
| `affinity.podAntiAffinityType` | `required` — local has exactly 2 workers for 2 instances, so enforcement works | `required` — unchanged; just ensure schedulable node count ≥ instance count before increasing instances |
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

## Redis

ProperTea runs two Redis instances that must remain separate in prod. They have different durability requirements and must not share config.

| Instance | Service | Purpose | Eviction policy | Persistence |
|---|---|---|---|---|
| `redis` | `redis.redis:6379` | Sessions + ASP.NET Core Data Protection keys | `noeviction` — writes fail loudly if full, never silently drop keys | Enabled (`appendonly yes`) |
| `redis-cache` | `redis-cache.redis:6379` | Application cache (future use) | `allkeys-lru` — any key can be evicted freely | Disabled — cache misses are acceptable, PVC eliminates RWO reattachment delay |

Do not merge these into a single instance. `allkeys-lru` with persistence would silently discard Data Protection keys or session tokens when memory fills, producing subtle auth failures with no error log.

| Setting | Local | Prod |
|---|---|---|
| `redis` replica count | `1` | Consider Redis Sentinel (3 nodes) if session loss on node failure is unacceptable |
| `redis-cache` replica count | `1` | Single replica is fine — cache is stateless by design |
| `redis` persistence | `appendonly yes`, Longhorn RWO | Ensure Longhorn has replica count ≥ 2; RWO attach delay (~2 min) on node failure is acceptable for sessions |

## Kubernetes Node Failure Response

| Setting | Local value (applied) | Cloud (AKS) | Notes |
|---|---|---|---|
| `kube-apiserver --default-not-ready-toleration-seconds` | `120` | `60` (AKS default) | Time after node goes NotReady before pods are evicted. Default `300` is too slow for realistic single-node failures |
| `kube-apiserver --default-unreachable-toleration-seconds` | `120` | `60` (AKS default) | Same, for the Unreachable taint |

On AKS these are already set to 60s by the platform — do not override. On self-hosted Talos, `120` is set in `deploy/environments/local/cluster/patches/cp-01.yaml`. This has no effect on full-cluster crash recovery (all nodes down simultaneously); it only helps the single-node-failure scenario where the control plane is healthy and can reschedule pods.
