#!/usr/bin/env bash
# Upgrades Talos OS and/or the Kubernetes control plane on the running cluster.
#
# Run this when a new Talos or Kubernetes version is available. Always upgrade
# one minor version at a time (e.g. Kubernetes 1.30 → 1.31, never 1.30 → 1.32).
#
# Compatibility matrix: https://www.talos.dev/latest/introduction/support-matrix/
#
# Three modes:
#   os       - Upgrade Talos OS on all nodes (control plane first, then workers)
#   k8s      - Upgrade Kubernetes components (after Talos OS is on the target version)
#   all      - OS upgrade then k8s upgrade
#
# Usage (from the cluster directory):
#   bash scripts/upgrade-talos.sh os  [TALOS_VERSION]
#   bash scripts/upgrade-talos.sh k8s [K8S_VERSION]
#   bash scripts/upgrade-talos.sh all [TALOS_VERSION] [K8S_VERSION]
#
# Examples:
#   bash scripts/upgrade-talos.sh os  v1.12.5
#   bash scripts/upgrade-talos.sh k8s 1.32.2
#   bash scripts/upgrade-talos.sh all v1.12.5 1.32.2

set -euo pipefail

MODE="${1:-}"
if [[ -z "$MODE" ]]; then
  echo "Usage: $0 [os|k8s|all] [versions...]" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_DIR="$(dirname "$SCRIPT_DIR")"
TALOSCONFIG="$CLUSTER_DIR/_out/talosconfig"

CP_NODE="192.168.50.10"
WORKER_NODES=("192.168.50.11" "192.168.50.12")

# Read schematic ID from common.yaml install.image line
SCHEMATIC=$(grep 'install.image:' "$CLUSTER_DIR/patches/common.yaml" 2>/dev/null \
  | sed 's|.*factory.talos.dev/installer/||;s|:.*||' || echo "")

if [[ -z "$SCHEMATIC" ]]; then
  echo "Error: could not read schematic ID from patches/common.yaml" >&2
  echo "Expected: machine.install.image: factory.talos.dev/installer/<schematic>:<version>" >&2
  exit 1
fi

upgrade_os() {
  local VERSION="$1"
  local IMAGE="factory.talos.dev/installer/${SCHEMATIC}:${VERSION}"

  echo "=== Upgrading Talos OS to ${VERSION} ==="
  echo "  Image: ${IMAGE}"
  echo "  Schematic: ${SCHEMATIC}"
  echo ""

  echo "--- Control plane: ${CP_NODE} ---"
  talosctl upgrade \
    --nodes "$CP_NODE" \
    --endpoints "$CP_NODE" \
    --talosconfig "$TALOSCONFIG" \
    --image "$IMAGE" \
    --wait

  echo "  Control plane upgraded. Waiting for API to be ready..."
  sleep 10
  talosctl version --nodes "$CP_NODE" --endpoints "$CP_NODE" --talosconfig "$TALOSCONFIG"

  for WORKER in "${WORKER_NODES[@]}"; do
    echo ""
    echo "--- Worker: ${WORKER} ---"
    talosctl upgrade \
      --nodes "$WORKER" \
      --endpoints "$CP_NODE" \
      --talosconfig "$TALOSCONFIG" \
      --image "$IMAGE" \
      --wait
    echo "  Done."
  done

  echo ""
  echo "All nodes upgraded to ${VERSION}."
  echo ""
  echo "IMPORTANT: Update machine.install.image in patches/common.yaml to:"
  echo "  factory.talos.dev/installer/${SCHEMATIC}:${VERSION}"
  echo "Then commit and push."
}

upgrade_k8s() {
  local K8S_VERSION="$1"

  echo "=== Upgrading Kubernetes to ${K8S_VERSION} ==="
  echo ""
  echo "--- Dry run ---"
  talosctl upgrade-k8s \
    --nodes "$CP_NODE" \
    --endpoints "$CP_NODE" \
    --talosconfig "$TALOSCONFIG" \
    --to "$K8S_VERSION" \
    --dry-run

  echo ""
  read -p "Proceed with upgrade? [y/N] " CONFIRM
  if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
    echo "Aborted."
    exit 0
  fi

  talosctl upgrade-k8s \
    --nodes "$CP_NODE" \
    --endpoints "$CP_NODE" \
    --talosconfig "$TALOSCONFIG" \
    --to "$K8S_VERSION"

  echo ""
  echo "Kubernetes upgraded to ${K8S_VERSION}."
  kubectl get nodes -o wide
}

case "$MODE" in
  os)
    TALOS_VERSION="${2:?Usage: $0 os <talos-version> (e.g. v1.12.5)}"
    upgrade_os "$TALOS_VERSION"
    ;;
  k8s)
    K8S_VERSION="${2:?Usage: $0 k8s <k8s-version> (e.g. 1.32.2)}"
    upgrade_k8s "$K8S_VERSION"
    ;;
  all)
    TALOS_VERSION="${2:?Usage: $0 all <talos-version> <k8s-version>}"
    K8S_VERSION="${3:?Usage: $0 all <talos-version> <k8s-version>}"
    upgrade_os "$TALOS_VERSION"
    echo ""
    upgrade_k8s "$K8S_VERSION"
    ;;
  *)
    echo "Error: unknown mode '$MODE'. Use: os, k8s, or all" >&2
    exit 1
    ;;
esac
