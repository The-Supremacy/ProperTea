# Local Kubernetes Strategy (Talos)

Local Talos Linux cluster designed to mirror a production AKS deployment as closely as possible. Primary goal is learning; secondary goal is infrastructure parity so manifests transfer to AKS with minimal changes.

## Cluster Topology

| Node | Role | vCPU | RAM | Disk |
|---|---|---|---|---|
| `cp-01` | Control plane | 2 | 4 GB | 40 GB |
| `worker-01` | Worker | 2 | 8 GB | 60 GB |
| `worker-02` | Worker | 2 | 8 GB | 60 GB |
| **Total** | | **6** | **20 GB** | **160 GB** |

Control plane is tainted `NoSchedule` — only runs etcd, kube-apiserver, kube-scheduler, kube-controller-manager, and the Cilium agent. All application workloads schedule on workers.

Talos nodes are stateless and survive cold boot cleanly. VMs can be shut down and restarted at will. etcd handles clean restarts. Stateful workloads use Local Path PVs that persist across reboots.

### Estimated Workload Distribution (Workers)

| Category | Components | Est. RAM |
|---|---|---|
| Observability | VictoriaMetrics, Loki, Tempo, Grafana, OTel Collector | ~2.5 GB |
| GitOps / Infra | ArgoCD, cert-manager, SOPS | ~500 MB |
| Stateful deps | PostgreSQL (CloudNativePG), Redis, RabbitMQ | ~1.5 GB |
| Identity | ZITADEL, OpenFGA | ~500 MB |
| Application | 4 microservices + BFF + Angular (nginx) | ~800 MB |
| Operators | KEDA, Argo Rollouts, Reloader | ~300 MB |
| Node overhead | kubelet, Cilium agent, Hubble, kube-proxy replacement | ~400 MB/node |

Use `podAntiAffinity` to spread replicas of stateful services across both workers.

---

## Phase 1: CI, Containerization, and Versioning

Predictably version and package all services before they touch Kubernetes.

### Conventional Commits + Release Please

Adopt [Conventional Commits](https://www.conventionalcommits.org/) across the repository (`feat:`, `fix:`, `chore:`).

Use [Release Please](https://github.com/googleapis/release-please) in GitHub Actions to parse commit history, bump semantic versions (Major.Minor.Patch), and generate changelogs automatically.

### Container Images

**Backend (.NET services) -- no Dockerfiles.** Use the .NET SDK built-in container publishing. Each service `.csproj` declares its image name:

```xml
<PropertyGroup>
  <ContainerImageName>ghcr.io/yourorg/propertea-company</ContainerImageName>
  <ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0</ContainerBaseImage>
</PropertyGroup>
```

CI publishes via:

```bash
dotnet publish apps/services/ProperTea.Company \
  /t:PublishContainer \
  -c Release \
  -p ContainerImageTag=$VERSION
```

Repeat per service. No Dockerfile needed anywhere for .NET.

**Services to containerize:**

| Service | Image Name |
|---|---|
| `ProperTea.Organization` | `ghcr.io/<org>/propertea-organization` |
| `ProperTea.Company` | `ghcr.io/<org>/propertea-company` |
| `ProperTea.Property` | `ghcr.io/<org>/propertea-property` |
| `ProperTea.User` | `ghcr.io/<org>/propertea-user` |
| `ProperTea.Landlord.Bff` | `ghcr.io/<org>/propertea-landlord-bff` |

**Angular frontend -- single Dockerfile.** Standard multi-stage build: `ng build` stage copies dist output into `nginx:alpine`. One Dockerfile at `apps/portals/landlord/web/Dockerfile`.

### Tagging Strategy

Tag every image with both:
- Semantic version: `v1.2.3`
- Git commit SHA: `abc1234`

Use `docker/metadata-action` in the GitHub Actions workflow for clean multi-tag handling.

### GitHub Actions Workflow

Trigger on new release tags created by Release Please. Build all changed service images in a matrix, push to GHCR.

---

## Phase 2: Talos VM Bootstrapping and Local Infrastructure

### Provision Talos

Spin up 3 VMs (1 CP + 2 workers) via QEMU/libvirt or Proxmox. Bootstrap using `talosctl`.

Use `talosctl stats`, `talosctl logs`, and `talosctl dmesg` from the host machine to monitor raw VM health and bootstrapping. No SSH available or needed.

### CNI: Cilium (Single Networking Layer)

Install Cilium as the sole networking component. It replaces Flannel, MetalLB, and Envoy Gateway:

| Concern | Cilium Feature |
|---|---|
| CNI + Network Policy | Core Cilium |
| L2 Load Balancer | L2 announcements (replaces MetalLB) |
| Gateway API (HTTPRoute) | Cilium Gateway API (stable since 1.15) |
| Network Observability | Hubble |

Enable Hubble during initial Cilium Helm install. `hubble observe` provides real-time network flow visibility.

No separate Envoy Gateway, MetalLB, or Nginx Ingress needed.

### Storage

Install [Local Path Provisioner](https://github.com/rancher/local-path-provisioner). Allows stateful services to claim persistent storage on the Talos VM's disk. PVs survive reboots.

---

## Phase 3: GitOps and Secrets Management

### ArgoCD

Install ArgoCD in the cluster. Operates on a pull model -- reaches out to GitHub to read manifests. No inbound internet exposure required.

Chosen over Flux v2 because:
- More popular, larger community
- Built-in UI for resource graph, live diff, sync status, rollback -- valuable for learning
- ApplicationSets manage all services from a single declarative object

### Infrastructure Repository

Create a dedicated GitHub repository (e.g., `local-k8s-infra`) to hold all cluster state. ArgoCD manages itself from this repo (App of Apps pattern or ApplicationSets).

### Secret Management: SOPS + age

Chosen over Sealed Secrets because secrets are not tied to a specific cluster controller key. If the cluster is destroyed, secrets remain recoverable with the age key.

Workflow:
1. Encrypt secrets locally using `sops` with an `age` public key.
2. Commit encrypted files to the infra repo.
3. ArgoCD decrypts on apply using the age private key (stored as a K8s secret bootstrapped manually once).
4. Use [Helm Secrets plugin](https://github.com/jkroepke/helm-secrets) for ArgoCD integration.

For AKS migration: replace age backend with Azure Key Vault via External Secrets Operator (ESO).

---

## Phase 4: Networking and Certificates

### Gateway API via Cilium

Write standard `Gateway` and `HTTPRoute` manifests to route traffic. Cilium handles all gateway functionality. This maps to the Managed Gateway API pattern on Azure.

Weighted `HTTPRoute` backends enable percentage-based canary routing:

```yaml
rules:
  - backendRefs:
    - name: my-service-stable
      weight: 90
    - name: my-service-canary
      weight: 10
```

### Certificate Management

Install `cert-manager`. Configure DNS-01 challenge with a real domain via Cloudflare:
- Own a real domain (e.g., `*.k8s.yourdomain.com`).
- Provide cert-manager with a Cloudflare API token (encrypted via SOPS).
- Let's Encrypt issues real trusted certificates for your local environment.

---

## Phase 5: Stateful Dependencies and Identity

Deploy all backing services via ArgoCD from the infra repo. All stateful services bind to the Local Path StorageClass.

### PostgreSQL: CloudNativePG

Use [CloudNativePG](https://cloudnative-pg.io/) operator instead of Bitnami Helm charts. Handles HA, backups, replicas. Closer to production patterns and what you'd run on AKS.

ZITADEL and all Marten-backed services (Organization, Company, Property, User) each get their own database on this Postgres cluster.

### Redis

Helm chart. Used by the Landlord BFF for session storage.

### RabbitMQ

Helm chart. Used by Wolverine for message routing and integration events between services.

### ZITADEL

Deploy via Helm. Connect to CloudNativePG Postgres instance. Credentials decrypted by SOPS. Public-facing UI mapped through a Cilium `HTTPRoute`.

### OpenFGA

Deploy via Helm. Used for relationship-based access control (ADR 0004, ADR 0008).

---

## Phase 6: Observability

### Metrics: VictoriaMetrics

Chosen over Thanos (too many components for local scale) and Mimir (same concern). VictoriaMetrics advantages:
- Single-binary mode: one pod, Prometheus-compatible scraping.
- Native Azure Blob Storage support via `vmbackup` (AKS parity).
- Grafana connects identically to Prometheus -- no dashboard changes.

Local: store metrics on Local Path PV (in-memory retention is sufficient).
AKS: same `vmoperator` manifests, swap storage backend to Azure Blob.

### Logs: Loki

Grafana Loki for log aggregation. Deployed via Helm.

### Traces: Tempo

Grafana Tempo for distributed trace storage. Services already emit OpenTelemetry via Aspire's `ProperTea.ServiceDefaults`.

### Dashboards: Grafana

Central dashboard for metrics (VictoriaMetrics), logs (Loki), and traces (Tempo).

### OpenTelemetry Collector

Deploy as a DaemonSet. Receives OTel signals from all services and fans out to:
- VictoriaMetrics (metrics)
- Loki (logs)
- Tempo (traces)

This mirrors the Azure Monitor OTel pipeline. Existing `ProperTea.ServiceDefaults` instrumentation works without service code changes -- only the collector endpoint configuration changes between local and AKS.

---

## Phase 7: Application Deployment

### Argo Rollouts (Canary / Blue-Green)

Install Argo Rollouts for progressive delivery:
- Integrates with ArgoCD (UI plugin).
- Uses Gateway API for traffic splitting (works with Cilium).
- Supports canary + blue-green strategies.
- Can gate promotion on VictoriaMetrics queries (e.g., "don't advance if error rate > 1%").

### Service Manifests

Push Kubernetes manifests for all services to the infra repo. ArgoCD detects changes, pulls images from GHCR, deploys pods.

Services deployed:

| Service | Type | Notes |
|---|---|---|
| `propertea-organization` | Deployment + Service | Wolverine handler, Marten |
| `propertea-company` | Deployment + Service | Wolverine handler, Marten |
| `propertea-property` | Deployment + Service | Wolverine handler, Marten |
| `propertea-user` | Deployment + Service | Wolverine handler, Marten |
| `propertea-landlord-bff` | Deployment + Service | YARP reverse proxy, Redis sessions |
| `propertea-landlord-web` | Deployment + Service | nginx serving Angular SPA |

Cilium `HTTPRoute` rules route HTTPS traffic to the BFF and Angular frontend.

---

## Additional Operators

| Operator | Purpose | Priority |
|---|---|---|
| **KEDA** | Event-driven pod autoscaling. Scales Wolverine consumers based on RabbitMQ queue depth. AKS has a managed KEDA add-on -- direct parity. | High |
| **Argo Rollouts** | Progressive delivery (canary, blue-green) with metric-gated promotion. | High |
| **CloudNativePG** | PostgreSQL operator (HA, backups, replicas). | High |
| **Reloader** | Watches ConfigMaps/Secrets, triggers rolling restarts on change. | Medium |
| **Velero** | Cluster backup and restore (etcd + PVs). Supports Azure Blob as backend on AKS. | Medium |
| **VPA** | Vertical Pod Autoscaler. Run in recommendation mode first to right-size resource requests. | Low |

### Not Needed Locally

| Tool | Why Not |
|---|---|
| **Karpenter** | Provisions/decommissions cloud VMs reactively. Nothing to do on fixed local nodes. |
| **Cluster Autoscaler** | Same reason as Karpenter. |
| **MetalLB** | Cilium L2 announcements replace this. |
| **Envoy Gateway / Nginx Ingress** | Cilium Gateway API replaces these. |
| **Istio** | Cilium covers networking, observability (Hubble), and Gateway API without the mesh overhead. |

---

## AKS Migration Path

The local-to-AKS delta is intentionally small:

| Concern | Local | AKS |
|---|---|---|
| Node provisioning | Manual VMs | AKS managed node pools + Karpenter |
| Load balancer | Cilium L2 | Azure Load Balancer |
| Gateway | Cilium Gateway API | Azure Managed Gateway / Cilium |
| Storage | Local Path Provisioner | Azure Managed Disks / Azure Files |
| Secrets | SOPS + age | External Secrets Operator + Azure Key Vault |
| Metrics long-term | VictoriaMetrics local PV | VictoriaMetrics + Azure Blob (`vmbackup`) |
| Logs long-term | Loki local PV | Loki + Azure Blob |
| DNS/Certs | cert-manager + Cloudflare | cert-manager + Azure DNS (or Cloudflare) |
| GitOps | ArgoCD | ArgoCD (identical) |

Application manifests, Helm values, and ArgoCD ApplicationSets remain the same. Only infrastructure-layer backends change.
