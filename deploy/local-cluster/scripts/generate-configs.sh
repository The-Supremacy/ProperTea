#!/usr/bin/env bash
# Generates Talos machine configs from secrets + patches.
# Run from the deploy/local-cluster/ directory.
#
# Prerequisites: talosctl installed
# Usage:
#   ./scripts/generate-configs.sh           # generates new secrets + configs
#   ./scripts/generate-configs.sh existing  # reuses existing secrets.yaml

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PATCHES_DIR="$ROOT_DIR/patches"
OUT_DIR="$ROOT_DIR/_out"
SECRETS_FILE="$ROOT_DIR/secrets.yaml"

CLUSTER_NAME="propertea-cluster"
CP_ENDPOINT="https://192.168.50.10:6443"

# Generate or reuse secrets
if [[ "${1:-}" == "existing" ]]; then
  if [[ ! -f "$SECRETS_FILE" ]]; then
    echo "Error: secrets.yaml not found. Run without 'existing' to generate."
    exit 1
  fi
  echo "Reusing existing secrets.yaml"
else
  echo "Generating new secrets..."
  talosctl gen secrets -o "$SECRETS_FILE"
fi

# Clean output directory
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

echo "Generating control plane config (cp-01)..."
talosctl gen config "$CLUSTER_NAME" "$CP_ENDPOINT" \
  --with-secrets "$SECRETS_FILE" \
  --config-patch @"$PATCHES_DIR/common.yaml" \
  --config-patch-control-plane @"$PATCHES_DIR/cp-01.yaml" \
  --output "$OUT_DIR/controlplane.yaml" \
  --output-types controlplane

echo "Generating worker config (worker-01)..."
talosctl gen config "$CLUSTER_NAME" "$CP_ENDPOINT" \
  --with-secrets "$SECRETS_FILE" \
  --config-patch @"$PATCHES_DIR/common.yaml" \
  --config-patch-worker @"$PATCHES_DIR/worker-01.yaml" \
  --output "$OUT_DIR/worker-01.yaml" \
  --output-types worker

echo "Generating worker config (worker-02)..."
talosctl gen config "$CLUSTER_NAME" "$CP_ENDPOINT" \
  --with-secrets "$SECRETS_FILE" \
  --config-patch @"$PATCHES_DIR/common.yaml" \
  --config-patch-worker @"$PATCHES_DIR/worker-02.yaml" \
  --output "$OUT_DIR/worker-02.yaml" \
  --output-types worker

# Also generate talosconfig for the client
talosctl gen config "$CLUSTER_NAME" "$CP_ENDPOINT" \
  --with-secrets "$SECRETS_FILE" \
  --output "$OUT_DIR/talosconfig" \
  --output-types talosconfig

echo ""
echo "Configs generated in $OUT_DIR/"
echo ""
echo "Next steps (run on infra VM or via port-forwarded endpoints):"
echo ""
echo "  1. Apply configs to running Talos nodes:"
echo "     talosctl apply-config --insecure --nodes 192.168.50.10 --file $OUT_DIR/controlplane.yaml"
echo "     talosctl apply-config --insecure --nodes 192.168.50.11 --file $OUT_DIR/worker-01.yaml"
echo "     talosctl apply-config --insecure --nodes 192.168.50.12 --file $OUT_DIR/worker-02.yaml"
echo ""
echo "  2. Bootstrap etcd on control plane:"
echo "     talosctl bootstrap --nodes 192.168.50.10 --endpoints 192.168.50.10 --talosconfig $OUT_DIR/talosconfig"
echo ""
echo "  3. Get kubeconfig:"
echo "     talosctl kubeconfig --nodes 192.168.50.10 --endpoints 192.168.50.10 --talosconfig $OUT_DIR/talosconfig"
echo ""
echo "  See deploy/local-cluster/README.md for full instructions."
