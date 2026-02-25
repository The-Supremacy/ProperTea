#!/usr/bin/env bash
# Installs ArgoCD with the SOPS/age CMP sidecar for encrypted secret support.
#
# What this sets up:
#   - age keypair at ~/.config/sops/age/keys.txt (generated once, keep safe)
#   - argocd-age-key K8s secret in argocd namespace (used by the CMP sidecar)
#   - ArgoCD v3.x via Helm with a repo-server sidecar that decrypts *.enc.yaml
#     files at sync time using sops + age
#
# Prerequisites:
#   - kubectl pointing at the cluster
#   - helm installed
#   - age installed on this machine (the infra VM) -- for age-keygen
#     sops belongs on the dev machine (for encrypting secrets before committing to Git)
#   - deploy/infrastructure/base/argocd/values.yaml exists
#
# Usage (from the cluster directory -- consistent with the other bootstrap scripts):
#   bash scripts/install-argocd.sh [local]
#
# Defaults to 'local' if no argument is supplied.

set -euo pipefail

ENV="${1:-local}"
if [[ "$ENV" != "local" ]]; then
  echo "Error: ENV must be 'local' (got: '$ENV')" >&2
  echo "Usage: $0 [local]" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_DIR="$(dirname "$SCRIPT_DIR")"           # scripts/ -> cluster/
ENV_DIR="$(dirname "$CLUSTER_DIR")"               # cluster/ -> environments/<env>/
ENVIRONMENTS_DIR="$(dirname "$ENV_DIR")"          # environments/<env>/ -> environments/
DEPLOY_DIR="$(dirname "$ENVIRONMENTS_DIR")"       # environments/ -> deploy/
REPO_ROOT="$(dirname "$DEPLOY_DIR")"              # deploy/ -> repo root
ARGOCD_MANIFESTS="$REPO_ROOT/deploy/infrastructure/base/argocd"

ARGOCD_VERSION="9.4.4"
AGE_KEY_FILE="${HOME}/.config/sops/age/keys.txt"
ENV_VALUES="$REPO_ROOT/deploy/environments/$ENV/argocd/values.yaml"

echo "Installing ArgoCD for environment: $ENV"

# ---- Step 1: age keypair ----
echo "=== Step 1: age keypair ==="
if [[ -f "$AGE_KEY_FILE" ]]; then
  echo "  Key already exists at $AGE_KEY_FILE"
  PUBLIC_KEY=$(grep "^# public key:" "$AGE_KEY_FILE" | awk '{print $4}')
  echo "  Public key: $PUBLIC_KEY"
else
  echo "  Generating new age keypair..."
  mkdir -p "$(dirname "$AGE_KEY_FILE")"
  age-keygen -o "$AGE_KEY_FILE"
  PUBLIC_KEY=$(grep "^# public key:" "$AGE_KEY_FILE" | awk '{print $4}')
  echo "  Public key: $PUBLIC_KEY"
  echo ""
  echo "  IMPORTANT: Back up $AGE_KEY_FILE -- losing it means losing access to all"
  echo "  encrypted secrets. The public key in .sops.yaml can be freely committed."
fi

# Sync .sops.yaml with the actual public key. This is a no-op if the key
# already matches, but prevents stale-key mismatches if the keypair is
# regenerated or if install-argocd.sh is run on a different machine.
SOPS_YAML="$REPO_ROOT/.sops.yaml"
CURRENT=$(grep "^    age:" "$SOPS_YAML" | awk '{print $2}')
if [[ "$CURRENT" != "$PUBLIC_KEY" ]]; then
  echo "  Updating .sops.yaml public key ($CURRENT → $PUBLIC_KEY)"
  sed -i "s|^    age: .*|    age: $PUBLIC_KEY|" "$SOPS_YAML"
else
  echo "  .sops.yaml already has the correct public key."
fi

# ---- Step 2: argocd namespace ----
echo ""
echo "=== Step 2: argocd namespace ==="
kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -

# ---- Step 3: age key K8s secret ----
echo ""
echo "=== Step 3: argocd-age-key secret ==="
kubectl create secret generic argocd-age-key \
  --namespace argocd \
  --from-literal=key.txt="$(cat "$AGE_KEY_FILE")" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  argocd-age-key secret applied."

# ---- Step 4: ArgoCD Helm install ----
echo ""
echo "=== Step 4: ArgoCD ${ARGOCD_VERSION} ==="
helm repo add argo https://argoproj.github.io/argo-helm 2>/dev/null || true
helm repo update argo 2>/dev/null

# --server-side tells Helm to use server-side apply instead of client-side apply.
# Without this, Helm stores the full manifest in the last-applied-configuration
# annotation, which exceeds the 262144-byte K8s limit for ArgoCD's large CRDs
# and causes ArgoCD self-sync to fail with "Too long" errors.
# Requires Helm 4+.
helm upgrade --install argocd argo/argo-cd \
  --version "$ARGOCD_VERSION" \
  --namespace argocd \
  --values "$ARGOCD_MANIFESTS/values.yaml" \
  --values "$ENV_VALUES" \
  --server-side \
  --wait \
  --timeout 10m

# ---- Step 5: Initial password ----
echo ""
echo "=== Done ==="
echo ""
echo "ArgoCD is running. Initial credentials:"
echo "  Username: admin"
PASS=$(kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" 2>/dev/null | base64 -d)
echo "  Password: ${PASS:-<secret rotated -- check argocd-initial-admin-secret>}"
echo ""
echo "Access the UI (temporary port-forward until Gateway is configured):"
echo "  POD=\$(kubectl get pod -n argocd -l app.kubernetes.io/name=argocd-server -o jsonpath='{.items[0].metadata.name}')"
echo "  kubectl port-forward pod/\$POD -n argocd 8080:8080 --address 0.0.0.0 &"
echo "  open http://<infra-vm-ip>:8080"
echo ""
echo "Next: register the GitHub deploy key, then bootstrap GitOps:"
echo "  1. Add the following public key to https://github.com/The-Supremacy/ProperTea/settings/keys"
echo "     (read-only, title: 'ArgoCD ${ENV} cluster')"
echo "  2. Run: bash scripts/bootstrap-gitops.sh $ENV"
