# ProperTea Deploy Environments

This directory contains one folder per environment. Each environment manages its own ArgoCD App-of-Apps root and all manifests/values specific to that environment. `../infrastructure/base/` contains shared base Helm values and manifests that environments layer on top of.

## Environment Taxonomy

| Environment | Domain | TLS | Cluster | Status |
|---|---|---|---|---|
| `local/` | `*.local` | Self-signed CA (cert-manager) | Talos KVM on `propertea-k8s-local` | Active |
| `uat/` | `*.uat.propertea.io` | Let's Encrypt | AKS | Not yet provisioned |
| `prod/` | `*.propertea.io` | Let's Encrypt | AKS | Not yet provisioned |

## Bootstrap

```bash
# One-time per cluster — run from deploy/environments/local/cluster/
bash scripts/install-argocd.sh local
bash scripts/bootstrap-gitops.sh local
kubectl apply -f ../root-app.yaml
```

After `bootstrap-gitops.sh`, all cluster changes are driven from Git. ArgoCD reconciles everything in sync-wave order automatically.

## Directory Structure

```
environments/
  local/
    root-app.yaml              # Apply once to bootstrap local env
    apps/                      # ArgoCD Applications (one per component, sync-wave ordered)
    cluster/                   # Talos bootstrap scripts + machine configs
    argocd/                    # ArgoCD HTTPRoute + env Helm values
    cert-manager/              # Self-signed CA ClusterIssuers
    gateway/                   # Cilium L2 pool + Gateway resource
    longhorn/                  # Longhorn HTTPRoute + env Helm values
    zitadel/                   # ZITADEL env Helm values + InfisicalSecret CR
    zitadel-route/             # ZITADEL HTTPRoute (separate so wave 7 can apply it)
    infisical/                 # Infisical env Helm values + SOPS-encrypted secrets
  uat/                         # Stub — AKS, not yet provisioned
  prod/                        # Stub — AKS, not yet provisioned
```

## Infrastructure Library

`../infrastructure/base/` contains base Helm values and manifests shared across environments:

- `argocd/values.yaml` — SOPS CMP sidecar, Dex disabled, insecure mode
- `longhorn/values.yaml` — replica balance, storage threshold
- `zitadel/values.yaml` — base OIDC/SAML config, DB connection structure
- `infisical/values.yaml` — bundled postgres/redis, Longhorn PVCs, no ingress-nginx

Environment-specific values files are merged on top via ArgoCD's `helm.valueFiles` list.

## Adding a New Service

1. Add a base manifest + `values.yaml` under `../infrastructure/base/<service>/`.
2. Add an ArgoCD Application to `environments/<env>/apps/` with the correct `sync-wave` annotation.
3. For env-specific overrides, add `values.yaml` under `environments/<env>/<service>/`.
