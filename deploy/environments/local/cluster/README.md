# Local Talos Cluster Setup

Reference and step-by-step bootstrap guide for the 3-node Talos Kubernetes cluster used as the local development environment.

## Host Architecture

```
Windows PC (Hyper-V host -- stays clean, no dev tools)
└── propertea-k8s-local  (Ubuntu 24.04, 8 vCPU, 24 GB RAM -- this machine)
    talosctl / kubectl / helm all run here
    ├── talos-cp-01      (KVM guest, 192.168.50.10, 2 vCPU, 4 GB, control plane)
    ├── talos-worker-01  (KVM guest, 192.168.50.11, 2 vCPU, 8 GB, worker)
    └── talos-worker-02  (KVM guest, 192.168.50.12, 2 vCPU, 8 GB, worker)
```

`propertea-k8s-local` is the KVM hypervisor, VS Code SSH target, and cluster management node. All tooling (`talosctl`, `kubectl`, `helm`) runs here. The Windows PC is the Hyper-V platform only.

All Talos nodes are on the `talos-net` libvirt NAT bridge (`virbr-talos`, `192.168.50.0/24`). This host is the NAT gateway at `192.168.50.1`. libvirt sets up NAT masquerade automatically. No port forwarding needed.

### Infra VM Sizing

| Component | RAM |
|---|---|
| Ubuntu host overhead | ~1.5 GB |
| talos-cp-01 | 4 GB |
| talos-worker-01 | 8 GB |
| talos-worker-02 | 8 GB |
| **Total** | **~22 GB** |

Assign the infra VM: 8 vCPUs, 24 GB RAM, 200 GB disk. Enable nested virtualisation in Hyper-V.

### Cluster Topology

| Node | Role | vCPU | RAM | Disk |
|---|---|---|---|---|
| `talos-cp-01` | Control plane | 2 | 4 GB | 40 GB |
| `talos-worker-01` | Worker | 2 | 8 GB | 60 GB |
| `talos-worker-02` | Worker | 2 | 8 GB | 60 GB |

Control plane is tainted `NoSchedule` -- only runs etcd, kube-apiserver, kube-scheduler, kube-controller-manager, and the Cilium agent. All application workloads schedule on workers.

Talos nodes are stateless -- VMs can be shut down and restarted freely. etcd handles clean restarts. Stateful data lives on Longhorn PVs backed by worker node disks and persists across reboots.

### Networking and Services

Cilium is the sole networking layer: CNI, kube-proxy replacement, L2 load balancer, and Gateway API. Services are exposed via LoadBalancer IPs on `192.168.50.200-210` with Cilium L2 announcements (ARP).

TLS is terminated at the Cilium Gateway. cert-manager issues a wildcard `*.local` certificate from a self-signed local CA (`propertea-ca-issuer`).

## Prerequisites

On `propertea-k8s-local` (the infra VM):
- `talosctl`, `kubectl` installed
- `helm` **v4+** installed -- `install-argocd.sh` uses `--server-side` (Helm 4 feature)
  ```bash
  curl -fsSL https://get.helm.sh/helm-v4.1.1-linux-amd64.tar.gz | sudo tar xz -C /usr/local/bin --strip-components=1 linux-amd64/helm
  ```
- `age` installed -- used by `install-argocd.sh` to generate the SOPS age keypair (`age-keygen`)
  ```bash
  sudo apt install -y age
  ```

On the **dev machine** (where you edit and commit code):
- `sops` -- for encrypting secret files locally before committing them to Git
  ```bash
  # Download from https://github.com/getsops/sops/releases
  SOPS_VERSION="v3.12.1"
  wget -qO /usr/local/bin/sops \
    "https://github.com/getsops/sops/releases/download/${SOPS_VERSION}/sops-${SOPS_VERSION}.linux.amd64"
  chmod +x /usr/local/bin/sops
  ```

On the Windows PC:
- `propertea-k8s-local` VM created (see Step 1) with nested virtualisation enabled
- Talos ISO downloaded on `propertea-k8s-local` (see Step 3):
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
cd ~/repos/ProperTea/deploy/environments/local/cluster
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
cd ~/repos/ProperTea/deploy/environments/local/cluster
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
cd ~/repos/ProperTea/deploy/environments/local/cluster
bash scripts/bootstrap-cluster.sh
```

Nodes will be `NotReady` at the end -- expected until Cilium is installed.

## Step 8: Install Cilium and Storage

Run the infrastructure script:

```bash
bash scripts/install-infrastructure.sh
```

This installs Gateway API CRDs, Cilium 1.19.1 (with Talos-specific settings), Local Path Provisioner (as a bootstrap stop-gap default StorageClass), and kubelet-csr-approver. All nodes will be `Ready` when it completes.

> **Storage note:** Local Path Provisioner covers the window between ArgoCD install and Longhorn becoming available (e.g. ArgoCD's own Redis PVC). Once GitOps is bootstrapped, ArgoCD installs Longhorn 1.7.2 via the `longhorn` Application — Longhorn then becomes the default StorageClass. Local Path Provisioner remains available for non-replicated workloads (no configuration needed; keep it as-is).

> **Longhorn + Talos:** Longhorn requires `iscsi-tools` and `util-linux-tools` system extensions and a kubelet `extraMounts` entry for `/var/lib/longhorn`. Both are handled in `patches/common.yaml`. The patch also pins `machine.install.image` to the schematic-keyed installer — without this, Talos installs the generic image to disk during bootstrap and loses extensions on the first reboot even though the ISO had them. If you bump the Talos version or schematic, update the `install.image` in `common.yaml` to match.

> **Cilium + Talos:** Cilium requires `cgroup.autoMount.enabled=false` and explicit capability sets. Without these the `clean-cilium-state` init container fails with a capabilities error. The script includes these settings.

## Step 9: Install ArgoCD

Installs ArgoCD with the SOPS/age CMP sidecar for encrypted secret support:

```bash
bash scripts/install-argocd.sh local
```

The script installs ArgoCD using two values files layered together:
- `deploy/infrastructure/base/argocd/values.yaml` — base config (SOPS CMP sidecar, insecure mode)
- `deploy/environments/local/argocd/values.yaml` — env-specific overrides (domain: `argocd.local`)

Access the UI temporarily via port-forward (stopgap until the Cilium Gateway is live):

```bash
kubectl port-forward svc/argocd-server -n argocd 8080:80 &
# open http://localhost:8080
```

> **Note:** `kubectl port-forward` tunnels directly to a pod and has no resilience -- it exits if the pod restarts. Run the command again to reconnect. The Gateway setup eliminates this permanently.

## Step 10: Bootstrap Infisical

Infisical is used for application secrets. The Infisical Helm release (`infisical.yaml`, wave 5) and the Infisical Kubernetes Operator (`secrets-operator.yaml`, wave 5) are applied manually as part of the wave-by-wave bootstrap in Step 12.

Keycloak does **not** require Infisical for bootstrap. It generates all cryptographic material itself on first startup and stores it in the database. Admin credentials for local dev are committed in plaintext in `deploy/environments/local/keycloak/values.yaml`.

Once wave 5 is synced and Infisical is reachable at `https://infisical.local`:

1. Create a user account and organisation (e.g. `ProperTea Local`)
2. Create a project to hold future application secrets
3. Set up Machine Identities as needed when adding services that require Infisical-managed secrets

## Step 11: Bootstrap GitOps

This is the last manual `kubectl apply`. After it, all cluster changes are driven from Git.

```bash
bash scripts/bootstrap-gitops.sh local
```

The script will:
1. Generate an SSH deploy key and print the public key
2. Wait for you to add the key to `github.com/The-Supremacy/ProperTea/settings/keys` (read-only)
3. Register the repo credential in ArgoCD
4. Apply `deploy/environments/local/root-app.yaml` — the root Application

ArgoCD then syncs `deploy/environments/local/apps/` directly, in sync-wave order:

```
local-apps  (watches deploy/environments/local/apps/)
  ├── argocd.yaml              ← wave 0: ArgoCD self-manages its Helm release
  ├── argocd-config.yaml       ← wave 1: HTTPRoute for argocd.local
  ├── cert-manager.yaml        ← wave 1: cert-manager Helm install
  ├── cert-manager-config.yaml ← wave 2: ClusterIssuers (self-signed local CA)
  ├── gateway-config.yaml      ← wave 3: Cilium L2 pool, Gateway, HTTPRoute
  ├── longhorn.yaml            ← wave 3: Longhorn Helm install (becomes default StorageClass)
  ├── longhorn-config.yaml     ← wave 4: Longhorn UI HTTPRoute
  ├── cloudnativepg.yaml       ← wave 4: CloudNativePG operator
  ├── infisical.yaml           ← wave 5: Infisical Helm install + HTTPRoute (auto-sync, SOPS-decrypted secret)
  ├── secrets-operator.yaml    ← wave 5: Infisical Kubernetes Operator (watches InfisicalSecret CRs)
  ├── keycloak-config.yaml     ← wave 6: keycloak namespace + CNPG Cluster CR
  └── keycloak.yaml            ← wave 7: Keycloak Bitnami Helm install + HTTPRoute
```

Each cluster environment runs its own independent ArgoCD instance, with its own root app pointing only at its environment's apps. `deploy/infrastructure/` is a shared values library referenced by Applications via the `$values` pattern — it contains no Applications itself.

To update ArgoCD config after bootstrap:
- Base settings (SOPS CMP sidecar, insecure mode): `deploy/infrastructure/base/argocd/values.yaml`
- Local overrides (domain): `deploy/environments/local/argocd/values.yaml`

## Step 12: Sync Wave-by-Wave

All ArgoCD Applications have `automated.enabled: false`. This is intentional: ArgoCD wave ordering only gates when a sync *starts*, not when the upstream resources are *actually ready*. With auto-sync on, a later wave can begin before the previous wave's resources are truly healthy (e.g. CNPG reports the Cluster CR as Healthy the moment it is accepted by the API server, before any PostgreSQL pod is Running).

Sync each wave manually, verify real health before proceeding to the next.

```bash
# Helper: trigger a sync for one app
kubectl patch application <name> -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
```

### Wave 0 — ArgoCD self-manages

```bash
kubectl patch application argocd -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":false}}}'
# Wait for ArgoCD pods to settle
kubectl rollout status deployment argocd-server -n argocd
```

### Wave 1 — cert-manager + ArgoCD HTTPRoute

```bash
kubectl patch application cert-manager -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":false}}}'
kubectl patch application argocd-config -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":false}}}'
# Verify cert-manager webhook is ready before continuing
kubectl wait --for=condition=Available deployment/cert-manager-webhook -n cert-manager --timeout=120s
```

### Wave 2 — ClusterIssuers

```bash
kubectl patch application cert-manager-config -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
kubectl get clusterissuer -o wide  # both should be Ready
```

### Wave 3 — Gateway + Longhorn

```bash
kubectl patch application gateway -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
kubectl patch application longhorn -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
# Wait for Longhorn to finish initialisation (can take 2-3 min)
kubectl wait --for=condition=Available deployment/longhorn-manager -n longhorn-system --timeout=300s
```

### Wave 4 — Longhorn HTTPRoute + CloudNativePG operator

```bash
kubectl patch application longhorn-config -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
kubectl patch application cloudnativepg -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":false}}}'
# Wait for the CNPG controller to be ready — its CRDs must be established before wave 6
kubectl wait --for=condition=Available deployment/cnpg-controller-manager -n cnpg-system --timeout=180s
```

### Wave 5 — Infisical + Secrets Operator

```bash
kubectl patch application infisical -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
kubectl patch application secrets-operator -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
kubectl get pods -n infisical -w  # wait until Running
```

### Wave 6 — Keycloak DB (CNPG Cluster)

```bash
kubectl patch application keycloak-config -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
# Do NOT proceed to wave 7 until the CNPG Cluster is fully Ready.
# The ArgoCD Application will show Healthy before the DB pods are even scheduling.
# Wait for real readiness:
kubectl wait --for=condition=Ready cluster/keycloak-db -n keycloak --timeout=300s
# Verify the app secret was created:
kubectl get secret keycloak-db-app -n keycloak
```

### Wave 7 — Keycloak

```bash
kubectl patch application keycloak -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'
# Keycloak takes ~2 minutes on first boot (DB schema init + crypto key generation)
kubectl rollout status statefulset/keycloak -n keycloak --timeout=300s
# Access at https://keycloak.local — admin / Password1!
```

### Re-enabling auto-sync after successful first boot

Once everything is running, you can opt back in to automation per-app. Edit any Application and set `automated.enabled: true`. Keep `prune: true` on apps that own namespaced resources; keep it `false` on `argocd` itself to avoid self-disruption.

## Updating Machine Config on Running Nodes

To push patch changes to a running cluster (not during initial bootstrap):

```bash
cd ~/repos/ProperTea/deploy/environments/local/cluster
./scripts/generate-configs.sh existing  # regenerate without new secrets

talosctl apply-config --nodes 192.168.50.10 --endpoints 192.168.50.10 --talosconfig _out/talosconfig --file _out/controlplane.yaml
talosctl apply-config --nodes 192.168.50.11 --endpoints 192.168.50.11 --talosconfig _out/talosconfig --file _out/worker-01.yaml
talosctl apply-config --nodes 192.168.50.12 --endpoints 192.168.50.12 --talosconfig _out/talosconfig --file _out/worker-02.yaml
```

## Upgrading

### Cilium

Cilium is installed by `install-infrastructure.sh` before GitOps is bootstrapped, so it is not managed by ArgoCD. Upgrade it manually:

```bash
bash scripts/upgrade-cilium.sh 1.19.1
```

The script exports the current installed values and re-applies them against the new chart version (avoids `--reuse-values` nil pointer errors from new required keys). After upgrading, update `CILIUM_VERSION` in `install-infrastructure.sh` and commit.

### Talos OS + Kubernetes

Never skip minor versions — upgrade one at a time:

```bash
# Show current versions
talosctl version --nodes 192.168.50.10 --talosconfig _out/talosconfig
kubectl version

# Upgrade (control plane first, then workers, then Kubernetes)
bash scripts/upgrade-k8s.sh v1.12.5 1.32.2
```

The script reads the schematic ID from `patches/common.yaml` automatically — only the version tag changes. After upgrading, update `machine.install.image` in `patches/common.yaml` to the new version and commit.

Compatibility matrix: https://www.talos.dev/latest/introduction/support-matrix/

## Monitoring Talos Nodes

Nodes are reachable directly at their internal IPs:

```bash
talosctl dashboard --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl logs     --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl stats    --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl dmesg    --nodes 192.168.50.10 --talosconfig _out/talosconfig
talosctl service  --nodes 192.168.50.10 --talosconfig _out/talosconfig
```

## Accessing Cluster Services from Windows

Services are exposed via Cilium's L2 LoadBalancer on the `192.168.50.200-210` IP pool. The `virbr-talos` bridge is internal to `propertea-k8s-local` — Windows cannot reach it by default. This one-time setup routes traffic through the infra VM.

### One-time setup (per dev machine)

**1. Allow forwarding through the libvirt firewall** (on `propertea-k8s-local`):

libvirt adds REJECT rules to the FORWARD chain that block external-to-bridge traffic. Insert ACCEPT rules before them:

```bash
sudo iptables -I FORWARD 1 -i eth0 -o virbr-talos -j ACCEPT
sudo iptables -I FORWARD 2 -i virbr-talos -o eth0 -j ACCEPT
sudo iptables -t nat -A POSTROUTING -o virbr-talos -j MASQUERADE
```

Make persistent across reboots:

```bash
sudo apt install -y iptables-persistent
sudo netfilter-persistent save
```

**2. Add a static route on Windows** (PowerShell as Administrator):

```powershell
# Replace 172.28.26.190 with the actual IP of propertea-k8s-local on the Windows-facing network
# Find it with: ip addr show eth0
route add 192.168.50.0 mask 255.255.255.0 172.28.26.190 -p
```

The `-p` flag makes the route persistent across Windows reboots.

### Per service (each new service exposed via HTTPRoute)

Add an entry to `C:\Windows\System32\drivers\etc\hosts` (Notepad as Administrator):

```
192.168.50.200 argocd.local
192.168.50.200 keycloak.local
192.168.50.200 app.local
# Add one line per service hostname
```

All services share the same gateway IP (`192.168.50.200`). Cilium routes to the correct backend service based on the `Host` header.

### Verify

```bash
# From propertea-k8s-local — gateway should respond (self-signed cert, so -k)
curl -sk -o /dev/null -w "%{http_code}" https://argocd.local
# Expect 200

# From Windows PowerShell — node should be reachable
ping 192.168.50.10
```

Then open `https://argocd.local` in a browser on Windows.

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
deploy/
  environments/local/
    cluster/                    # Local-only bootstrap tooling (future envs use Terraform)
      README.md                 # This file
      secrets.yaml              # GENERATED, git-ignored -- cluster secrets
      _out/                     # GENERATED, git-ignored -- machine configs
      patches/
        common.yaml             # Shared: Cilium CNI skip, DNS, NTP
        cp-01.yaml              # Control plane: static IP
        worker-01.yaml          # Worker 1: static IP
        worker-02.yaml          # Worker 2: static IP
      scripts/
        generate-configs.sh       # Generates machine configs from secrets + patches
        create-talos-vms.sh       # Creates all 3 KVM guests (--destroy to tear down)
        bootstrap-cluster.sh      # Applies configs, bootstraps etcd, fetches kubeconfig
        install-infrastructure.sh # Gateway API CRDs, Cilium, Local Path Provisioner, kubelet-csr-approver
        install-argocd.sh         # age key, ArgoCD Helm install with SOPS CMP sidecar
        bootstrap-gitops.sh       # SSH deploy key, repo credential, apply root-app.yaml (run once)
        upgrade-cilium.sh         # Upgrades Cilium in-place (not ArgoCD-managed)
        upgrade-k8s.sh            # Upgrades Talos OS + Kubernetes (sequential minor versions)

    root-app.yaml               # Bootstrap: manually applied once. Watches environments/local/apps/
    apps/                       # All ArgoCD Applications for this cluster
      argocd.yaml
      argocd-config.yaml
      cert-manager.yaml
      cert-manager-config.yaml
      cloudnativepg.yaml
      gateway-config.yaml
      longhorn-config.yaml
      longhorn.yaml
      keycloak-config.yaml
      keycloak.yaml
    argocd/
      values.yaml               # ArgoCD env-specific overrides (domain: argocd.local)
      kustomization.yaml        # Patches base HTTPRoute hostname
      httproute-patch.yaml
    longhorn/
      values.yaml               # Longhorn env-specific overrides (replicas: 2, default StorageClass: true)
      kustomization.yaml
      httproute-patch.yaml
    keycloak/
      values.yaml               # Keycloak env-specific overrides (hostname, DB connection, admin seed)
      kustomization.yaml
    keycloak-route/
      httproute.yaml            # HTTPRoute for keycloak.local
    cert-manager/
      cluster-issuer.yaml       # Self-signed CA (prod uses ACME + Cloudflare DNS01)
    gateway/
      gateway.yaml              # Cilium L2 IP pool + Gateway (*.local, self-signed TLS)

  infrastructure/
    base/                       # Kustomize base manifests (env-agnostic)
      argocd/
        kustomization.yaml
        httproute.yaml          # HTTPRoute with hostname: argocd.placeholder
        values.yaml             # ArgoCD base Helm values (SOPS CMP, insecure mode)
      longhorn/
        kustomization.yaml
        httproute.yaml
        values.yaml             # Longhorn base Helm values (replica balance, storage threshold)
      keycloak/
        kustomization.yaml
        postgres-cluster.yaml   # CloudNativePG Cluster (canonical)
        httproute.yaml          # HTTPRoute base (hostname: keycloak.placeholder)
        values.yaml             # Keycloak base Helm values (proxy headers, cache, DB structure)
      infisical/
        kustomization.yaml
        httproute.yaml
        values.yaml             # Infisical base Helm values (bundled postgres/redis, Longhorn PVCs)
    aks/                        # Future AKS infrastructure stubs
```

## AKS Migration Path

The local-to-AKS delta is intentionally small. Application manifests, `HTTPRoute` definitions, Helm values, and ArgoCD `Application` objects are the same across environments -- only the infrastructure-layer backends change:

| Concern | Local | AKS |
|---|---|---|
| Node provisioning | Manual KVM VMs | AKS managed node pools + Karpenter |
| Load balancer | Cilium L2 | Azure Load Balancer (Cilium cloud LB) |
| Gateway | Cilium Gateway API | Cilium Gateway API (same manifests) |
| Storage | Local Path Provisioner + Longhorn | Azure Managed Disks |
| Secrets (runtime) | Manual / Infisical Operator | ESO + Azure Key Vault |
| Secrets (Git) | SOPS + age | SOPS + age (identical) |
| Metrics | VictoriaMetrics (local PV) | VictoriaMetrics + Azure Blob |
| Logs | Loki (local PV) | Loki + Azure Blob |
| DNS / Certs | cert-manager + self-signed CA | cert-manager + Cloudflare DNS-01 / ACME |
| GitOps | ArgoCD | ArgoCD (identical) |

## Next Steps

After GitOps is bootstrapped, all further changes go through Git.

## Daily Operations Cheatsheet

### ArgoCD UI Access

Port-forward to the **Service** (survives pod restarts):

```bash
kubectl port-forward svc/argocd-server -n argocd 8080:80 --address 0.0.0.0 &
# open http://localhost:8080
```

> Once HTTPRoutes are wired to real hostnames this is no longer needed.

Get the initial admin password:

```bash
kubectl get secret argocd-initial-admin-secret -n argocd -o jsonpath='{.data.password}' | base64 -d && echo
```

### ArgoCD Application Status

```bash
# Overview of all apps
kubectl get applications -n argocd

# Health + sync status for one app
kubectl get application infisical -n argocd -o jsonpath='{.status.sync.status} {.status.health.status}' && echo

# Which resources are OutOfSync
kubectl get application infisical -n argocd -o json \
  | python3 -c "import sys,json; d=json.load(sys.stdin); [print(r['kind'], r.get('name',''), r['status']) for r in d['status'].get('resources',[])]"

# Full event/error log for an app
kubectl describe application infisical -n argocd | tail -40
```

### ArgoCD Manual Sync

```bash
# Trigger sync (with prune) via patch -- no argocd CLI needed
kubectl patch application infisical -n argocd --type merge \
  -p '{"operation":{"initiatedBy":{"username":"admin"},"sync":{"revision":"HEAD","prune":true}}}'

# Force a hard refresh (clears cache, re-fetches Git)
kubectl patch application infisical -n argocd --type merge \
  -p '{"metadata":{"annotations":{"argocd.argoproj.io/refresh":"hard"}}}'
```

### Helm Diagnostics

```bash
# Render chart locally with values (dry-run what ArgoCD would apply)
helm template <release> <repo>/<chart> --version <ver> --values path/to/values.yaml

# Example: render Infisical with base values
helm template infisical infisical-helm-charts/infisical-standalone --version 1.7.2 \
  --values deploy/infrastructure/self-hosted/infisical/values.yaml \
  | grep -E "^(kind|  name:)"

# List what values a chart exposes
helm show values <repo>/<chart> --version <ver>

# Diff a live release against new values before upgrading
helm diff upgrade <release> <repo>/<chart> --values path/to/values.yaml
# (requires: helm plugin install https://github.com/databus23/helm-diff)

# Show values currently applied to a live release
helm get values <release> -n <namespace>
helm get values <release> -n <namespace> -o yaml > /tmp/current-values.yaml
```

### Kubernetes General

```bash
# Services in a namespace
kubectl get svc -n <namespace>

# All resources in a namespace
kubectl get all -n <namespace>

# Describe a resource (events + conditions)
kubectl describe pod <name> -n <namespace>

# Follow logs for a deployment
kubectl logs -n <namespace> -l app=<label> -f --tail=100

# Follow logs for a specific pod (previous crash)
kubectl logs <pod> -n <namespace> --previous

# Exec into a running container
kubectl exec -it <pod> -n <namespace> -- /bin/sh

# Watch pod status
kubectl get pods -n <namespace> -w

# Force-delete a stuck terminating pod
kubectl delete pod <pod> -n <namespace> --grace-period=0 --force
```

### CNPG (CloudNativePG)

```bash
# Cluster health
kubectl get cluster -n keycloak
kubectl describe cluster keycloak-db -n keycloak

# Primary pod
kubectl get pods -n keycloak -l cnpg.io/instanceRole=primary

# Connect to the database
kubectl exec -it -n keycloak \
  $(kubectl get pod -n keycloak -l cnpg.io/instanceRole=primary -o jsonpath='{.items[0].metadata.name}') \
  -- psql -U keycloak keycloak
```

### SOPS / Secrets

```bash
# Decrypt a SOPS-encrypted file to stdout
sops -d deploy/environments/local/infisical/infisical-secrets.enc.yaml

# Edit in-place (opens $EDITOR with decrypted content, re-encrypts on save)
sops deploy/environments/local/infisical/infisical-secrets.enc.yaml

# Encrypt a new file using the rules in .sops.yaml
sops -e -i path/to/new-file.enc.yaml
```
