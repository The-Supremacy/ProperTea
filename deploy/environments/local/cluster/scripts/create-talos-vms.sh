#!/usr/bin/env bash
# Creates 3 Talos KVM guests on the infra VM.
# Run on propertea-infra (the dedicated infra VM with libvirt).
#
# Prerequisites:
#   - libvirt, qemu-kvm, virtinst installed
#   - talos-net libvirt network defined and running
#   - Talos ISO at /var/lib/libvirt/images/talos-amd64.iso
#
# Usage:
#   ./create-talos-vms.sh                          # create VMs using default ISO path
#   ./create-talos-vms.sh /path/to/talos.iso       # create VMs using custom ISO path
#   ./create-talos-vms.sh --destroy                # destroy and undefine all 3 VMs

set -euo pipefail

NETWORK="talos-net"

# ---- Destroy mode ----
if [[ "${1:-}" == "--destroy" ]]; then
  echo "Destroying Talos VMs..."
  for NAME in talos-cp-01 talos-worker-01 talos-worker-02; do
    if virsh dominfo "$NAME" &>/dev/null; then
      virsh destroy "$NAME" 2>/dev/null || true   # force-off (no-op if already stopped)
      virsh undefine "$NAME" --remove-all-storage
      echo "  Destroyed: $NAME"
    else
      echo "  Not found, skipping: $NAME"
    fi
  done
  echo "Done."
  exit 0
fi

ISO="${1:-/var/lib/libvirt/images/talos-amd64.iso}"

if [[ ! -f "$ISO" ]]; then
  echo "Error: Talos ISO not found at $ISO"
  echo "Download it first (see README.md Step 3) or pass the path as an argument."
  echo "Usage: $0 [/path/to/talos.iso]"
  exit 1
fi

# Verify network exists
if ! virsh net-info "$NETWORK" &>/dev/null; then
  echo "Error: libvirt network '$NETWORK' not found."
  echo "Create it first (see README.md Step 2)."
  exit 1
fi

# VM definitions: Name, vCPU, RAM (MB), Disk (GB), MAC
VMS=(
  "talos-cp-01|2|4096|40|52:54:00:00:50:10"
  "talos-worker-01|2|8192|60|52:54:00:00:50:11"
  "talos-worker-02|2|8192|60|52:54:00:00:50:12"
)

for entry in "${VMS[@]}"; do
  IFS='|' read -r NAME VCPU RAM DISK MAC <<< "$entry"

  # Skip if VM already exists
  if virsh dominfo "$NAME" &>/dev/null; then
    echo "VM '$NAME' already exists, skipping."
    continue
  fi

  echo "Creating VM: $NAME (vCPU=$VCPU, RAM=${RAM}MB, Disk=${DISK}GB, MAC=$MAC)..."

  virt-install --name "$NAME" \
    --ram "$RAM" \
    --vcpus "$VCPU" \
    --disk "size=$DISK,format=qcow2" \
    --cdrom "$ISO" \
    --os-variant generic \
    --network "network=$NETWORK,mac=$MAC" \
    --boot uefi \
    --noautoconsole

  echo "  Created: $NAME"
done

echo ""
echo "All VMs created. Status:"
virsh list --all
echo ""
echo "Next: generate and apply Talos configs (see README.md Step 6-7)."
