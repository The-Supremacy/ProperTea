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
#   ./create-talos-vms.sh
#   ./create-talos-vms.sh /path/to/talos.iso

set -euo pipefail

ISO="${1:-/var/lib/libvirt/images/talos-amd64.iso}"
NETWORK="talos-net"

if [[ ! -f "$ISO" ]]; then
  echo "Error: Talos ISO not found at $ISO"
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
