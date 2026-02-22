# Local Talos Cluster Setup

Step-by-step guide to bootstrap a 3-node Talos Kubernetes cluster using nested KVM inside a dedicated Hyper-V infra VM.

## Architecture

```
Windows PC (Hyper-V host -- stays clean)
├── propertea-dev            (dev VM, VS Code SSH, talosctl/kubectl/helm)
└── propertea-infra          (infra VM, runs KVM)
    ├── talos-cp-01          (Talos control plane)
    ├── talos-worker-01      (Talos worker)
    └── talos-worker-02      (Talos worker)
```

Dev VM manages the cluster remotely over the Hyper-V Default Switch network. All Talos nodes live inside the infra VM, fully isolated from the host PC.

## Prerequisites

- Windows PC with Hyper-V enabled
- Existing dev VM (`propertea-dev`) with `talosctl`, `kubectl`, `helm` installed
- Talos ISO downloaded from [Talos Image Factory](https://factory.talos.dev/)
  - Architecture: amd64
  - Extensions: iscsi-tools, util-linux-tools
  - Secure Boot: off
  - Format: ISO

## Step 1: Create the Infra VM

In Hyper-V Manager, create `propertea-infra`:

| Setting | Value |
|---|---|
| Generation | 2 |
| vCPU | 8 |
| RAM | 24 GB (static, no dynamic memory) |
| Disk | 200 GB |
| Network | Default Switch |
| Secure Boot | Off |
| ISO | Ubuntu Server 24.04 LTS |

**Enable nested virtualization** (elevated PowerShell, VM must be stopped):

```powershell
Set-VMProcessor -VMName "propertea-infra" -ExposeVirtualizationExtensions $true
```

Install Ubuntu Server. After install, verify nested virt works:

```bash
# SSH into propertea-infra
egrep -c '(vmx|svm)' /proc/cpuinfo
# Should return > 0
```

## Step 2: Install KVM and Networking on Infra VM

SSH into `propertea-infra`:

```bash
sudo apt update && sudo apt install -y \
  qemu-kvm libvirt-daemon-system libvirt-clients \
  bridge-utils virtinst genisoimage

sudo systemctl enable --now libvirtd
sudo usermod -aG libvirt $USER
```

Log out and back in for the group change. Then create a bridged network for Talos nodes:

```bash
cat <<'EOF' | sudo tee /etc/libvirt/qemu/networks/talos-net.xml
<network>
  <name>talos-net</name>
  <forward mode="nat"/>
  <bridge name="virbr-talos" stp="on" delay="0"/>
  <ip address="192.168.50.1" netmask="255.255.255.0">
    <dhcp>
      <host mac="52:54:00:00:50:10" ip="192.168.50.10"/>
      <host mac="52:54:00:00:50:11" ip="192.168.50.11"/>
      <host mac="52:54:00:00:50:12" ip="192.168.50.12"/>
    </dhcp>
  </ip>
</network>
EOF

sudo virsh net-define /etc/libvirt/qemu/networks/talos-net.xml
sudo virsh net-start talos-net
sudo virsh net-autostart talos-net
```

This creates a NAT network with DHCP reservations. Talos nodes get predictable IPs via MAC address, so the static IP patches in machine configs are not strictly required but remain as a safety layer.

### IP Assignments

| Node | MAC | IP | Role |
|---|---|---|---|
| virbr-talos gateway | -- | 192.168.50.1 | NAT gateway (infra VM) |
| talos-cp-01 | 52:54:00:00:50:10 | 192.168.50.10 | Control plane |
| talos-worker-01 | 52:54:00:00:50:11 | 192.168.50.11 | Worker |
| talos-worker-02 | 52:54:00:00:50:12 | 192.168.50.12 | Worker |

## Step 3: Upload Talos ISO to Infra VM

From your local machine, copy the ISO:

```bash
scp talos-amd64.iso user@propertea-infra:/var/lib/libvirt/images/
```

## Step 4: Create Talos VMs

SSH into `propertea-infra` and run the script (or do it manually):

**Option A -- Script:**

Copy `deploy/local-cluster/scripts/create-talos-vms.sh` to the infra VM and run:

```bash
chmod +x create-talos-vms.sh
./create-talos-vms.sh
```

**Option B -- Manual:**

```bash
ISO=/var/lib/libvirt/images/talos-amd64.iso

# Control plane
virt-install --name talos-cp-01 \
  --ram 4096 --vcpus 2 \
  --disk size=40,format=qcow2 \
  --cdrom "$ISO" \
  --os-variant generic \
  --network network=talos-net,mac=52:54:00:00:50:10 \
  --boot uefi \
  --noautoconsole

# Worker 1
virt-install --name talos-worker-01 \
  --ram 8192 --vcpus 2 \
  --disk size=60,format=qcow2 \
  --cdrom "$ISO" \
  --os-variant generic \
  --network network=talos-net,mac=52:54:00:00:50:11 \
  --boot uefi \
  --noautoconsole

# Worker 2
virt-install --name talos-worker-02 \
  --ram 8192 --vcpus 2 \
  --disk size=60,format=qcow2 \
  --cdrom "$ISO" \
  --os-variant generic \
  --network network=talos-net,mac=52:54:00:00:50:12 \
  --boot uefi \
  --noautoconsole
```

Verify VMs are running:

```bash
virsh list --all
```

## Step 5: Expose Talos API to Dev VM

The Talos nodes are on a NAT network inside the infra VM. The dev VM can't reach `192.168.50.x` directly. Set up port forwarding on the infra VM using `socat` or `iptables`:

**Using socat (simpler):**

```bash
sudo apt install -y socat

# Forward talosctl API (port 50000) and K8s API (port 6443) to CP node
sudo socat TCP-LISTEN:50000,fork,reuseaddr TCP:192.168.50.10:50000 &
sudo socat TCP-LISTEN:6443,fork,reuseaddr TCP:192.168.50.10:6443 &
```

To make these persistent, create a systemd service (covered in `scripts/setup-port-forwards.sh`).

From the dev VM, use the **infra VM's Default Switch IP** as the endpoint:

```bash
# Find infra VM's IP (check on the infra VM)
hostname -I
# e.g., 172.x.x.x

# From dev VM
talosctl --nodes 172.x.x.x --endpoints 172.x.x.x version
```

## Step 6: Generate Talos Configs

On the **dev VM** (where the source code lives):

```bash
cd ~/src/ProperTea/deploy/local-cluster
./scripts/generate-configs.sh
```

This creates:
- `secrets.yaml` -- cluster secrets (**do not commit**)
- `_out/controlplane.yaml` -- control plane machine config
- `_out/worker-01.yaml` -- worker 1 machine config
- `_out/worker-02.yaml` -- worker 2 machine config
- `_out/talosconfig` -- client config for talosctl

To regenerate configs without new secrets (e.g., after changing patches):

```bash
./scripts/generate-configs.sh existing
```

## Step 7: Apply Configs

From the **dev VM**, apply configs via the infra VM's forwarded ports:

```bash
INFRA_IP=<propertea-infra IP on Default Switch>

# Apply to control plane (port-forwarded through infra VM)
talosctl apply-config --insecure --nodes $INFRA_IP --file _out/controlplane.yaml
```

For workers, you need additional port forwards on the infra VM (or apply from the infra VM directly):

```bash
# On infra VM: apply worker configs directly (192.168.50.x is reachable locally)
talosctl apply-config --insecure --nodes 192.168.50.11 --file _out/worker-01.yaml
talosctl apply-config --insecure --nodes 192.168.50.12 --file _out/worker-02.yaml
```

## Step 8: Bootstrap the Cluster

From the **dev VM**:

```bash
INFRA_IP=<propertea-infra IP>

# Bootstrap etcd (once, on control plane only)
talosctl bootstrap \
  --nodes $INFRA_IP \
  --endpoints $INFRA_IP \
  --talosconfig _out/talosconfig

# Get kubeconfig
talosctl kubeconfig \
  --nodes $INFRA_IP \
  --endpoints $INFRA_IP \
  --talosconfig _out/talosconfig
```

**Important:** Edit the generated kubeconfig to point at `$INFRA_IP:6443` (the port-forwarded address), not `192.168.50.10:6443`.

Verify:

```bash
kubectl get nodes
# Should show 3 nodes (NotReady until Cilium is installed)
```

## Step 9: Install Cilium

```bash
helm repo add cilium https://helm.cilium.io/
helm repo update

helm install cilium cilium/cilium \
  --namespace kube-system \
  --set ipam.mode=kubernetes \
  --set kubeProxyReplacement=true \
  --set k8sServiceHost=192.168.50.10 \
  --set k8sServicePort=6443 \
  --set hubble.enabled=true \
  --set hubble.relay.enabled=true \
  --set hubble.ui.enabled=true \
  --set gatewayAPI.enabled=true \
  --set l2announcements.enabled=true
```

Wait for Cilium to be ready:

```bash
kubectl -n kube-system wait --for=condition=Ready pod -l app.kubernetes.io/name=cilium-agent --timeout=120s
kubectl get nodes
# All nodes should now be Ready
```

## Step 10: Install Local Path Provisioner

```bash
kubectl apply -f https://raw.githubusercontent.com/rancher/local-path-provisioner/master/deploy/local-path-storage.yaml

kubectl patch storageclass local-path -p '{"metadata": {"annotations":{"storageclass.kubernetes.io/is-default-class":"true"}}}'
```

## Monitoring Talos Nodes

From the dev VM (via port-forwarded endpoints):

```bash
INFRA_IP=<propertea-infra IP>

talosctl dashboard --nodes $INFRA_IP --talosconfig _out/talosconfig
talosctl logs  --nodes $INFRA_IP --talosconfig _out/talosconfig
talosctl stats --nodes $INFRA_IP --talosconfig _out/talosconfig
talosctl dmesg --nodes $INFRA_IP --talosconfig _out/talosconfig
```

Or SSH into the infra VM and use the internal IPs directly:

```bash
talosctl dashboard --nodes 192.168.50.10 --talosconfig /path/to/talosconfig
```

## Shutting Down / Restarting

SSH into the infra VM:

```bash
# Graceful shutdown (workers first, then CP)
virsh shutdown talos-worker-02
virsh shutdown talos-worker-01
virsh shutdown talos-cp-01

# Restart (CP first, then workers)
virsh start talos-cp-01
virsh start talos-worker-01
virsh start talos-worker-02
```

Or stop the entire infra VM from Hyper-V -- all nested VMs hibernate with it.

## File Structure

```
deploy/local-cluster/
  README.md                   # This file
  secrets.yaml                # GENERATED, git-ignored -- cluster secrets
  _out/                       # GENERATED, git-ignored -- machine configs
  patches/
    common.yaml               # Shared: Cilium CNI skip, DNS, NTP
    cp-01.yaml                # Control plane: static IP
    worker-01.yaml            # Worker 1: static IP
    worker-02.yaml            # Worker 2: static IP
  scripts/
    generate-configs.sh       # Combines secrets + patches into machine configs
    create-talos-vms.sh       # Creates all 3 KVM guests on the infra VM
    setup-port-forwards.sh    # Persistent socat forwards for dev VM access
```

## Next Steps

After the cluster is running with Cilium + Local Path Provisioner, proceed with the phases in [local-k8s.md](../../docs/local-k8s.md):
- Phase 3: ArgoCD + SOPS
- Phase 4: Networking + cert-manager
- Phase 5: Stateful dependencies (CloudNativePG, Redis, RabbitMQ, ZITADEL)
- Phase 6: Observability (VictoriaMetrics, Loki, Tempo, Grafana)
- Phase 7: Application deployment
