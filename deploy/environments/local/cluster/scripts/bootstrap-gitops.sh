#!/usr/bin/env bash
# Connects ArgoCD to the GitHub repo and bootstraps the App of Apps GitOps structure.
#
# Run this ONCE after install-argocd.sh. After this, all cluster configuration
# is managed from Git -- including ArgoCD itself.
#
# What this does:
#   1. Generates an SSH deploy key for ArgoCD to authenticate with GitHub
#   2. Registers the repo credential as a K8s Secret in ArgoCD
#   3. Waits for you to add the public key to GitHub
#   4. Tests the connection
#   5. Applies the root Application (App of Apps) -- the last manual kubectl apply
#
# Prerequisites:
#   - ArgoCD is running (install-argocd.sh completed)
#   - kubectl pointing at the cluster
#   - ssh-keyscan available (openssh-client)
#
# Usage:
#   cd ~/repos/ProperTea
#   ./deploy/environments/local/cluster/scripts/bootstrap-gitops.sh [local|sit]
#
# Defaults to 'local' if no argument is supplied.

set -euo pipefail

ENV="${1:-local}"
if [[ "$ENV" != "local" && "$ENV" != "sit" ]]; then
  echo "Error: ENV must be 'local' or 'sit' (got: '$ENV')" >&2
  echo "Usage: $0 [local|sit]" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_DIR="$(dirname "$SCRIPT_DIR")"           # scripts/ -> cluster/
ENV_DIR="$(dirname "$CLUSTER_DIR")"               # cluster/ -> environments/<env>/
ENVIRONMENTS_DIR="$(dirname "$ENV_DIR")"          # environments/<env>/ -> environments/
DEPLOY_DIR="$(dirname "$ENVIRONMENTS_DIR")"       # environments/ -> deploy/
REPO_ROOT="$(dirname "$DEPLOY_DIR")"              # deploy/ -> repo root
DEPLOY_KEY_FILE="${HOME}/.config/argocd/deploy-key"

echo "Bootstrapping GitOps for environment: $ENV"
REPO_URL="git@github.com:The-Supremacy/ProperTea.git"
GITHUB_KEYS_URL="https://github.com/The-Supremacy/ProperTea/settings/keys"

# ---- Step 1: SSH deploy key ----
echo "=== Step 1: SSH deploy key ==="
if [[ -f "$DEPLOY_KEY_FILE" ]]; then
  echo "  Key already exists at $DEPLOY_KEY_FILE"
else
  mkdir -p "$(dirname "$DEPLOY_KEY_FILE")"
  ssh-keygen -t ed25519 -C "argocd@propertea-k8s-local" -f "$DEPLOY_KEY_FILE" -N "" -q
  echo "  Generated new deploy key."
fi
PUBLIC_KEY=$(cat "${DEPLOY_KEY_FILE}.pub")
echo ""
echo "  ┌─────────────────────────────────────────────────────────────┐"
echo "  │  Add this deploy key to GitHub (read-only):                 │"
echo "  │  $GITHUB_KEYS_URL"
echo "  │                                                             │"
echo "  │  Title: ArgoCD ${ENV} cluster                                  │"
echo "  │  Key:                                                       │"
echo "  │  $PUBLIC_KEY"
echo "  └─────────────────────────────────────────────────────────────┘"
echo ""
read -rp "  Press Enter once the deploy key is added to GitHub..."

# ---- Step 2: Register repo credential in ArgoCD ----
echo ""
echo "=== Step 2: ArgoCD repository credential ==="
GITHUB_HOST_KEY=$(ssh-keyscan -t ed25519 github.com 2>/dev/null)

kubectl create secret generic propertea-repo \
  --namespace argocd \
  --from-literal=type=git \
  --from-literal=url="$REPO_URL" \
  --from-literal=sshPrivateKey="$(cat "$DEPLOY_KEY_FILE")" \
  --from-literal=knownHosts="$GITHUB_HOST_KEY" \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl label secret propertea-repo \
  --namespace argocd \
  argocd.argoproj.io/secret-type=repository \
  --overwrite

echo "  Repository credential registered."

# ---- Step 3: Test connection ----
echo ""
echo "=== Step 3: Testing connection to GitHub ==="
# Give ArgoCD 10s to pick up the new secret, then test via SSH
sleep 5
if ssh -i "$DEPLOY_KEY_FILE" -o StrictHostKeyChecking=no -o BatchMode=yes \
     git@github.com 2>&1 | grep -q "successfully authenticated"; then
  echo "  SSH authentication to GitHub succeeded."
else
  echo "  Warning: could not verify SSH auth. Proceeding anyway."
  echo "  (ArgoCD will report a connection error in the UI if the key was not added.)"
fi

# ---- Step 4: Apply the root Application ----
echo ""
echo "=== Step 4: Bootstrapping App of Apps ==="
kubectl apply -f "$REPO_ROOT/deploy/environments/$ENV/root-app.yaml"
echo "  Root application applied."
echo ""
echo "  ArgoCD will now sync deploy/environments/$ENV/apps/ and manage:"
echo "    - cert-manager, argocd (self), gateway, cloudnativepg, zitadel, and their config"
echo "    - ArgoCD will self-manage its own Helm release once argocd.yaml is synced"
echo ""
echo "  Watch sync progress:"
echo "    kubectl get applications -n argocd -w"
echo "  Or open the ArgoCD UI:"
echo "    kubectl port-forward svc/argocd-server -n argocd 8080:80 &"
echo "    open http://localhost:8080"
echo ""
echo "  To update ArgoCD config after bootstrap:"
echo "    - Base values (SOPS CMP, insecure mode): deploy/infrastructure/helm/argocd/values.yaml"
echo "    - Env-specific overrides (domain, replicas):  deploy/environments/$ENV/argocd/values.yaml"
