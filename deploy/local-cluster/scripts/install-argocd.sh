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
#   - sops and age installed (see README)
#   - deploy/infrastructure/argocd/values.yaml exists
#
# Usage:
#   cd ~/repos/ProperTea
#   ./deploy/local-cluster/scripts/install-argocd.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$(dirname "$(dirname "$SCRIPT_DIR")")")"
ARGOCD_MANIFESTS="$REPO_ROOT/deploy/infrastructure/argocd"

ARGOCD_VERSION="9.4.4"
AGE_KEY_FILE="${HOME}/.config/sops/age/keys.txt"

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
echo "=== Step 4: ArgoCD ${ARGOCD_VERSION} =="
helm repo add argo https://argoproj.github.io/argo-helm 2>/dev/null || true
helm repo update argo 2>/dev/null

helm upgrade --install argocd argo/argo-cd \
  --version "$ARGOCD_VERSION" \
  --namespace argocd \
  --values "$ARGOCD_MANIFESTS/values.yaml" \
  --wait \
  --timeout 10m

# ---- Step 6: Initial password ----
echo ""
echo "=== Done ==="
echo ""
echo "ArgoCD is running. Access the UI:"
echo ""
echo "  kubectl port-forward svc/argocd-server -n argocd 8080:80 &"
echo "  open http://localhost:8080"
echo ""
echo "Initial credentials:"
echo "  Username: admin"
PASS=$(kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" 2>/dev/null | base64 -d)
echo "  Password: ${PASS:-<secret rotated -- check argocd-initial-admin-secret>}"
echo ""
echo "Change the password via the UI or CLI, then delete the initial secret:"
echo "  kubectl delete secret argocd-initial-admin-secret -n argocd"
