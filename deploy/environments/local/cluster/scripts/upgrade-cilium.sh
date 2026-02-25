#!/usr/bin/env bash
# Upgrades Cilium in-place to a target version.
#
# Cilium is NOT managed by ArgoCD -- it was installed by install-infrastructure.sh
# before GitOps was bootstrapped. Upgrades must be done manually with this script.
# After upgrading, update CILIUM_VERSION in install-infrastructure.sh and commit.
#
# Usage (from the cluster/ directory):
#   bash scripts/upgrade-cilium.sh [version]
#
# Defaults to the version pinned in install-infrastructure.sh if not provided.

set -euo pipefail

TARGET_VERSION="${1:-}"

if [[ -z "$TARGET_VERSION" ]]; then
  SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  TARGET_VERSION=$(grep '^CILIUM_VERSION=' "$SCRIPT_DIR/install-infrastructure.sh" | cut -d'"' -f2)
  echo "No version specified -- using pinned version from install-infrastructure.sh: $TARGET_VERSION"
fi

echo "=== Upgrading Cilium to ${TARGET_VERSION} ==="

# Dump current user-supplied values (not chart defaults) so we don't lose any
# cluster-specific settings like k8sServiceHost, l2announcements, etc.
helm get values cilium -n kube-system -o yaml > /tmp/cilium-upgrade-values.yaml
echo "  Current values exported to /tmp/cilium-upgrade-values.yaml"

helm repo update cilium 2>/dev/null

helm upgrade cilium cilium/cilium \
  --version "$TARGET_VERSION" \
  --namespace kube-system \
  --values /tmp/cilium-upgrade-values.yaml \
  --wait \
  --timeout 10m

echo ""
echo "=== Done: Cilium ${TARGET_VERSION} ==="
echo ""
echo "Verify: kubectl get pods -n kube-system -l k8s-app=cilium"
echo ""
echo "If CILIUM_VERSION in install-infrastructure.sh doesn't match, update it and commit:"
echo "  CILIUM_VERSION in scripts/install-infrastructure.sh"
