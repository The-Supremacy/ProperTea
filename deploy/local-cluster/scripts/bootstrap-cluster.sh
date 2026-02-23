#!/usr/bin/env bash
# Applies Talos machine configs to all 3 nodes and bootstraps the cluster.
# Run ONCE on first setup -- DO NOT re-run bootstrap on an existing cluster.
#
# Prerequisites:
#   - All 3 Talos VMs running and in maintenance mode (port 50000 open)
#   - Machine configs generated: ./generate-configs.sh
#
# Usage:
#   cd deploy/local-cluster
#   ./scripts/bootstrap-cluster.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_DIR="$(dirname "$SCRIPT_DIR")"
OUTDIR="$CLUSTER_DIR/_out"
TALOSCONFIG="$OUTDIR/talosconfig"

CP_IP="192.168.50.10"
WORKER1_IP="192.168.50.11"
WORKER2_IP="192.168.50.12"

if [[ ! -f "$TALOSCONFIG" ]]; then
  echo "Error: $TALOSCONFIG not found. Run ./scripts/generate-configs.sh first."
  exit 1
fi

# ---- Step 1: Wait for maintenance mode ----
echo "Waiting for nodes to enter maintenance mode (port 50000)..."
for IP in "$CP_IP" "$WORKER1_IP" "$WORKER2_IP"; do
  echo -n "  $IP: "
  for i in $(seq 1 30); do
    if nc -w 2 "$IP" 50000 2>/dev/null; then
      echo "ready"
      break
    fi
    if [[ $i -eq 30 ]]; then
      echo "ERROR: Timed out waiting for $IP"
      exit 1
    fi
    sleep 5
  done
done

# ---- Step 2: Apply machine configs ----
echo ""
echo "Applying machine configs..."
talosctl apply-config --insecure --nodes "$CP_IP"      --file "$OUTDIR/controlplane.yaml"
talosctl apply-config --insecure --nodes "$WORKER1_IP" --file "$OUTDIR/worker-01.yaml"
talosctl apply-config --insecure --nodes "$WORKER2_IP" --file "$OUTDIR/worker-02.yaml"
echo "Configs applied. Nodes are installing Talos and rebooting..."

# ---- Step 3: Wait for cp to come back up ----
echo ""
echo "Waiting for control plane to reboot and accept authenticated connections..."
sleep 15
for i in $(seq 1 30); do
  if talosctl version --nodes "$CP_IP" --endpoints "$CP_IP" --talosconfig "$TALOSCONFIG" &>/dev/null; then
    echo "  Control plane ready."
    break
  fi
  if [[ $i -eq 30 ]]; then
    echo "ERROR: Timed out waiting for control plane to come back up."
    exit 1
  fi
  sleep 10
done

# ---- Step 4: Bootstrap etcd ----
echo ""
echo "Bootstrapping etcd (this is a one-time operation)..."
talosctl bootstrap \
  --nodes "$CP_IP" \
  --endpoints "$CP_IP" \
  --talosconfig "$TALOSCONFIG"
echo "Bootstrap OK."

# ---- Step 5: Wait for Kubernetes API ----
echo ""
echo "Waiting for Kubernetes API (port 6443)..."
for i in $(seq 1 30); do
  if nc -w 3 "$CP_IP" 6443 2>/dev/null; then
    echo "  API server ready."
    break
  fi
  if [[ $i -eq 30 ]]; then
    echo "ERROR: Timed out waiting for K8s API."
    exit 1
  fi
  sleep 10
done

# ---- Step 6: Get kubeconfig ----
echo ""
echo "Fetching kubeconfig..."
talosctl kubeconfig \
  --nodes "$CP_IP" \
  --endpoints "$CP_IP" \
  --talosconfig "$TALOSCONFIG" \
  --force

echo ""
echo "Waiting for nodes to register..."
sleep 20
kubectl get nodes -o wide

echo ""
echo "Bootstrap complete. Nodes are NotReady until Cilium is installed."
echo "Next: ./scripts/install-infrastructure.sh"
