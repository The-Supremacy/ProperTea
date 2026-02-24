# ProperTea Deploy Environments

This directory contains one folder per environment. Each environment manages its own ArgoCD App-of-Apps root and all manifests/values specific to that environment. The `infrastructure/` library (one level up) contains shared base Helm values that environments layer on top of.

## Environment Taxonomy

| Environment | Domain | TLS | Cluster | Status |
|---|---|---|---|---|
| `local/` | `*.local` | Self-signed CA (cert-manager) | Talos KVM on `propertea-k8s-local` | Active |
| `sit/` | `*.sit.propertea.io` | Let's Encrypt (Cloudflare DNS-01) | Same Talos KVM cluster | Active (needs static IP) |
| `uat/` | `*.uat.propertea.io` | Let's Encrypt | AKS | Not yet provisioned |
| `prod/` | `*.propertea.io` | Let's Encrypt | AKS | Not yet provisioned |

Both `local` and `sit` point at the identical physical Talos cluster. Switching between them means telling ArgoCD to track a different root-app.yaml.

## Choosing an Environment

### local — works immediately, no static IP required

Use `local` when you want everything to work without a real domain or ISP static IP. All services are reachable via `*.local` hostnames on the KVM host and any machine that resolves those names (e.g. via `/etc/hosts` or a local DNS resolver).

Activate:

```bash
kubectl apply -f environments/local/root-app.yaml
```

### sit — real domain, Let's Encrypt via Cloudflare DNS-01

Use `sit` once you have a static IP and the `sit.propertea.io` NS records pointed at Cloudflare. cert-manager will obtain a wildcard certificate automatically via DNS-01.

Before activating:
1. Ensure `CLOUDFLARE_API_TOKEN` is SOPS-encrypted in `environments/sit/cert-manager/cloudflare-api-token.yaml`.
2. Confirm DNS delegation is live: `dig NS sit.propertea.io`.
3. Switch Let's Encrypt ACME server from staging to production in `environments/sit/cert-manager/cluster-issuer.yaml` when ready.

Activate:

```bash
kubectl apply -f environments/sit/root-app.yaml
```

## Switching Between Environments

Both root-app.yaml files create an ArgoCD Application named `local-apps` or `sit-apps` respectively. To switch:

1. Delete the current root app:

   ```bash
   kubectl delete application local-apps -n argocd   # or sit-apps
   ```

2. ArgoCD will garbage-collect all child Applications (because `finalizers: [resources-finalizer.argocd.argoproj.io]` is set).

3. Apply the new root app:

   ```bash
   kubectl apply -f environments/sit/root-app.yaml
   ```

> Only one root app should be active at a time against the same cluster. Running both simultaneously will cause resource conflicts (e.g. two Gateways in kube-system).

## Directory Structure

```
environments/
  local/
    root-app.yaml              # Apply once to bootstrap local env
    apps/                      # ArgoCD Applications (one per component)
    argocd-values.yaml         # ArgoCD Helm overrides (domain: argocd.local)
    longhorn-values.yaml       # Longhorn Helm overrides (replicas, storage class)
    zitadel-values.yaml        # ZITADEL Helm overrides (ExternalDomain, seed user)
    argocd/                    # ArgoCD HTTPRoute + IngressClass patch
    cert-manager/              # Self-signed CA chain ClusterIssuers
    gateway/                   # Cilium L2 pool + Gateway resource
    longhorn/                  # Longhorn HTTPRoute
    zitadel/                   # ZITADEL HTTPRoute + CloudNativePG cluster
    cluster/                   # Talos bootstrap scripts (shared with sit/)
  sit/
    root-app.yaml              # Apply once to bootstrap SIT env
    apps/
    argocd-values.yaml         # domain: argocd.sit.propertea.io
    longhorn-values.yaml
    zitadel-values.yaml        # ExternalDomain: zitadel.sit.propertea.io
    argocd/
    cert-manager/              # Let's Encrypt + Cloudflare DNS-01 ClusterIssuer
    gateway/                   # Same L2 pool; wildcard *.sit.propertea.io
    longhorn/
    zitadel/
    cluster/                   # Talos bootstrap scripts (source of truth)
  uat/                         # Stub — AKS, not yet provisioned
  prod/                        # Stub — AKS, not yet provisioned
```

## Infrastructure Library

`../infrastructure/` contains base Helm values that every environment layers on top of:

- `argocd/values.yaml` — SOPS CMP sidecar, Dex disabled, insecure mode
- `longhorn/values.yaml` — `replicaAutoBalance: least-effort`, `persistence.defaultClass: false`
- `zitadel/values.yaml` — base OIDC/SAML config, DB connection structure

Environment-specific `*-values.yaml` files are merged on top using ArgoCD's `helm.valueFiles` list.

## Adding a New Service

1. Add an ArgoCD Application manifest to `environments/<env>/apps/`.
2. Set the correct `wave` annotation to control ordering relative to deps.
3. If the service needs env-specific Helm values, add `<service>-values.yaml` to the environment root and reference it in the Application.
4. If it exposes HTTP, add an HTTPRoute manifest to `environments/<env>/<service>/`.
