# Local Talos Cluster Setup

Step-by-step guide to bootstrap a 3-node Talos Kubernetes cluster using nested KVM inside `propertea-k8s-local` (the dedicated KVM host VM). All tooling (`talosctl`, `kubectl`, `helm`) runs directly on this machine.

## Architecture

```
propertea-k8s-local  (this VM: Ubuntu 24.04, 8 vCPU, 24GB RAM, VS Code SSH)
  talosctl / kubectl / helm run here directly
  ├── talos-cp-01      (KVM guest, 192.168.50.10, control plane)
  ├── talos-worker-01  (KVM guest, 192.168.50.11, worker)
  └── talos-worker-02  (KVM guest, 192.168.50.12, worker)
```

All Talos nodes are on the `talos-net` libvirt NAT bridge (`virbr-talos`, `192.168.50.0/24`).
This host is the NAT gateway at `192.168.50.1`. libvirt automatically sets up NAT masquerade via iptables.
No port forwarding needed -- all `talosctl` and `kubectl` commands use `192.168.50.x` directly.

## Prerequisites

- Windows PC with Hyper-V enabled
- `propertea-k8s-local` VM created (see Step 1) with nested virtualisation enabled
- `talosctl`, `kubectl`, `helm` installed on `propertea-k8s-local` (not on the dev VM)
- Talos ISO downloaded directly on `propertea-k8s-local` (see Step 3)
  - Architecture: amd64
  - Extensions: `iscsi-tools`, `util-linux-tools`
  - Secure Boot: off
  - Format: ISO

## Step 1: Create the Infra VM

In Hyper-V Manager, create `propertea-k8s-local`:

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
Set-VMProcessor -VMName "propertea-k8s-local" -ExposeVirtualizationExtensions $true
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

## Step 3: Download Talos ISO

SSH into `propertea-k8s-local` and download the ISO directly. Get the schematic ID from [Talos Image Factory](https://factory.talos.dev/) by selecting:
- Platform: `metal`
- Architecture: `amd64`
- Extensions: `iscsi-tools`, `util-linux-tools`
- Secure Boot: off

The schematic ID is shown in the generated URL. Then download:

```bash
SCHEMATIC="613e1592b2da41ae5e265e8789429f22e121aab91cb4deb6bc3c0b6262961245"
VERSION="v1.12.4"

wget -O /var/lib/libvirt/images/talos-amd64.iso \
  "https://factory.talos.dev/image/${SCHEMATIC}/${VERSION}/metal-amd64.iso"
```

The schematic above includes `iscsi-tools` and `util-linux-tools`. If you change extensions, regenerate a new schematic ID at Image Factory.

## Step 4: Create Talos VMs

From the repo root on `propertea-k8s-local`, run the script (or do it manually):

**Option A -- Script:**

```bash
cd ~/repos/ProperTea/deploy/local-cluster
sudo bash scripts/create-talos-vms.sh
```

> Note: `sudo` is required unless you have logged out and back in after being added to the `libvirt` group in Step 2.

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

## Step 5: Verify NAT Connectivity

**This VM is the KVM host itself** -- `talosctl` and `kubectl` run directly here. The Talos nodes are on `192.168.50.x`, which is reachable from this host via the `virbr-talos` bridge. No port forwarding is needed.

Verify the libvirt NAT masquerade is working (libvirt sets this up automatically via `LIBVIRT_PRT` iptables chain):

```bash
sudo iptables -t nat -L LIBVIRT_PRT -n -v | grep 192.168.50
# Should show MASQUERADE rules for 192.168.50.0/24
```

If you see `network is unreachable` errors in Talos logs (via `talosctl logs machined`), add the rules manually:

```bash
EXT_IF=$(ip route show default | awk '/default/ {print $5}' | head -1)
sudo iptables -t nat -A POSTROUTING -s 192.168.50.0/24 -o "$EXT_IF" -j MASQUERADE
sudo iptables -A FORWARD -i virbr-talos -o "$EXT_IF" -j ACCEPT
sudo iptables -A FORWARD -i "$EXT_IF" -o virbr-talos -m state --state RELATED,ESTABLISHED -j ACCEPT
```

## Step 6: Generate Talos Configs

```bash
cd ~/repos/ProperTea/deploy/local-cluster
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

## Step 7: Apply Configs and Bootstrap

Run the bootstrap script. It will wait for maintenance mode, apply all configs, wait for reboot, bootstrap etcd, and fetch kubeconfig:

```bash
cd ~/repos/ProperTea/deploy/local-cluster
bash scripts/bootstrap-cluster.sh
```

Nodes will be `NotReady` at the end -- expected until Cilium is installed.

## Step 8: Install Cilium and Storage

Run the infrastructure script:

```bash
bash scripts/install-infrastructure.sh
```

This installs Gateway API CRDs, Cilium 1.17.2 (with Talos-specific settings), Local Path Provisioner, and kubelet-csr-approver. All nodes will be `Ready` when it completes.

> **Talos note:** Cilium requires `cgroup.autoMount.enabled=false` and explicit capability sets. Without these the `clean-cilium-state` init container fails with a capabilities error. The script includes these settings.

## Step 9: Install ArgoCD

Installs ArgoCD with the SOPS/age CMP sidecar for encrypted secret support:

```bash
bash scripts/install-argocd.sh
```

Access the UI temporarily via port-forward (this is a stopgap until the Cilium Gateway is configured in Phase 4):

```bash
kubectl port-forward svc/argocd-server -n argocd 8080:80 &
# open http://localhost:8080
```

> **Note:** `kubectl port-forward` tunnels directly to a pod and has no resilience -- it exits if the pod restarts. Run the command again to reconnect. The Gateway setup in Phase 4 eliminates this permanently.

## Step 10: Bootstrap GitOps

This is the last manual `kubectl apply`. After it, all cluster changes are driven from Git.

```bash
bash scripts/bootstrap-gitops.sh
```

The script will:
1. Generate an SSH deploy key and print the public key
2. Wait for you to add the key to `github.com/The-Supremacy/ProperTea/settings/keys` (read-only)
3. Register the repo credential in ArgoCD
4. Apply `deploy/infrastructure/root-app.yaml` -- the root Application that watches `deploy/infrastructure/apps/`

After this, ArgoCD syncs `apps/argocd.yaml`, which makes ArgoCD manage its own Helm release from Git. To update ArgoCD config from this point: edit `deploy/infrastructure/argocd/values.yaml` and push.

## Updating Machine Config on Running Nodes

To push patch changes to a running cluster (not during initial bootstrap):

```bash
cd ~/repos/ProperTea/deploy/local-cluster
./scripts/generate-configs.sh existing  # regenerate without new secrets

talosctl apply-config --nodes 192.168.50.10 --endpoints 192.168.50.10 --talosconfig _out/talosconfig --file _out/controlplane.yaml
talosctl apply-config --nodes 192.168.50.11 --endpoints 192.168.50.11 --talosconfig _out/talosconfig --file _out/worker-01.yaml
talosctl apply-config --nodes 192.168.50.12 --endpoints 192.168.50.12 --talosconfig _out/talosconfig --file _out/worker-02.yaml
```

## Monitoring Talos Nodes

Nodes are reachable directly at their internal IPs:

```bash
talosctl dashboard --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl logs     --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl stats    --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl dmesg    --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl service  --nodes 192.168.50.10 --talosconfig _out/talosconfig
```

## Shutting Down / Restarting

SSH into the infra VM:

```bash
# Graceful shutdown (workers first, then CP)
sudo virsh shutdown talos-worker-02
sudo virsh shutdown talos-worker-01
sudo virsh shutdown talos-cp-01

# Restart (CP first, then workers)
sudo virsh start talos-cp-01
sudo virsh start talos-worker-01
sudo virsh start talos-worker-02
```

> Or stop/start `propertea-k8s-local` from Hyper-V -- all nested VMs go down with it and resume on next boot. etcd will recover automatically once all nodes are back.



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
    generate-configs.sh       # Generates machine configs from secrets + patches
    create-talos-vms.sh       # Creates all 3 KVM guests
    bootstrap-cluster.sh      # Applies configs, bootstraps etcd, fetches kubeconfig
    install-infrastructure.sh # Gateway API CRDs, Cilium, Local Path Provisioner, kubelet-csr-approver
    install-argocd.sh         # age key, ArgoCD Helm install with SOPS CMP sidecar
    bootstrap-gitops.sh       # SSH deploy key, repo credential, apply root-app.yaml (run once)
```

## Next Steps

After GitOps is bootstrapped, all further changes go through Git. Proceed with the phases in [local-k8s.md](../../docs/local-k8s.md):
- Phase 4: cert-manager (self-signed for local, DNS-01 for prod) + Cilium Gateway
- Phase 5: Infisical + ESO, CloudNativePG, Redis, RabbitMQ, ZITADEL, OpenFGA
- Phase 6: Observability (VictoriaMetrics, Loki, Tempo, Grafana)
- Phase 7: Application deployment
