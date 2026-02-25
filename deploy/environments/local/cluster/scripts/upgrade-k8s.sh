#!/usr/bin/env bash
# Upgrades Talos OS and Kubernetes on all cluster nodes.
#
# IMPORTANT: Never skip minor versions.
#   - Talos:      upgrade one minor version at a time
#   - Kubernetes: upgrade one minor version at a time
#   Compatibility matrix: https://www.talos.dev/latest/introduction/support-matrix/
#
# The schematic ID (encodes extensions: iscsi-tools, util-linux-tools) is read
# from patches/common.yaml automatically. Only the Talos version tag changes.
#
# Usage (from the cluster/ directory):
#   bash scripts/upgrade-k8s.sh <talos-version> <k8s-version>
#
# Example:
#   bash scripts/upgrade-k8s.sh v1.12.5 1.32.2
#
# After upgrading, update machine.install.image in patches/common.yaml to the
# new version tag and commit, so future node provisioning uses the correct image.

set -euo pipefail

TALOS_VERSION="${1:-}"
K8S_VERSION="${2:-}"

if [[ -z "$TALOS_VERSION" || -z "$K8S_VERSION" ]]; then
  echo "Usage: $0 <talos-version> <k8s-version>" >&2
  echo "Example: $0 v1.12.5 1.32.2" >&2
  echo ""
  echo "Find compatible versions: https://www.talos.dev/latest/introduction/support-matrix/"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TALOSCONFIG="$SCRIPT_DIR/../_out/talosconfig"
COMMON_PATCH="$SCRIPT_DIR/../patches/common.yaml"

if [[ ! -f "$TALOSCONFIG" ]]; then
  echo "Error: _out/talosconfig not found. Run generate-configs.sh first." >&2
  exit 1
fi

# Extract schematic ID from the current install.image in common.yaml.
# The image URL is: factory.talos.dev/installer/<schematic-id>:<version>
SCHEMATIC_ID=$(grep -oP 'factory\.talos\.dev/installer/\K[a-f0-9]+' "$COMMON_PATCH" | head -1)
if [[ -z "$SCHEMATIC_ID" ]]; then
  echo "Error: could not extract schematic ID from $COMMON_PATCH" >&2
  echo "Expected a line like: machine.install.image: factory.talos.dev/installer/<id>:<version>" >&2
  exit 1
fi

INSTALLER_IMAGE="factory.talos.dev/installer/${SCHEMATIC_ID}:${TALOS_VERSION}"
CP_NODE="192.168.50.10"
WORKER_NODES=("192.168.50.11" "192.168.50.12")

echo "=== Talos + Kubernetes upgrade ==="
echo "    Talos target:    ${TALOS_VERSION}"
echo "    Kubernetes:      ${K8S_VERSION}"
echo "    Installer image: ${INSTALLER_IMAGE}"
echo "    Schematic ID:    ${SCHEMATIC_ID}"
echo ""

# ---- Step 1: Upgrade control plane ----
echo "--- Step 1: Upgrading control plane (${CP_NODE}) ---"
talosctl upgrade \
  --nodes "$CP_NODE" \
  --endpoints "$CP_NODE" \
  --talosconfig "$TALOSCONFIG" \
  --image "$INSTALLER_IMAGE" \
  --wait
echo "  Control plane upgraded."

# ---- Step 2: Upgrade workers ----
echo ""
echo "--- Step 2: Upgrading workers ---"
for NODE in "${WORKER_NODES[@]}"; do
  echo "  Upgrading ${NODE}..."
  talosctl upgrade \
    --nodes "$NODE" \
    --endpoints "$CP_NODE" \
    --talosconfig "$TALOSCONFIG" \
    --image "$INSTALLER_IMAGE" \
    --wait
  echo "  ${NODE} upgraded."
done

# ---- Step 3: Upgrade Kubernetes ----
echo ""
echo "--- Step 3: Upgrading Kubernetes to ${K8S_VERSION} ---"
talosctl upgrade-k8s \
  --nodes "$CP_NODE" \
  --endpoints "$CP_NODE" \
  --talosconfig "$TALOSCONFIG" \
  --to "$K8S_VERSION"

echo ""
echo "=== Done ==="
echo ""
echo "IMPORTANT: Update patches/common.yaml to pin the new installer image:"
echo "  machine.install.image: ${INSTALLER_IMAGE}"
echo "Then commit and push."
