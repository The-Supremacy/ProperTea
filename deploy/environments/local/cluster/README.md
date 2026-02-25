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

## Step 10: Bootstrap Infisical and ZITADEL Secrets

All secrets in this setup flow through Infisical. The Infisical Helm release (`infisical.yaml`, wave 5) and the Infisical Kubernetes Operator (`secrets-operator.yaml`, wave 5) both sync automatically — no manual steps needed for those.

### 10a: Wait for Infisical to be ready (wave 5)

`infisical-secrets` is committed as a SOPS-encrypted manifest (`infisical-secrets.enc.yaml`). ArgoCD's CMP sidecar decrypts it at apply time using the age private key in `argocd-age-key`. Wave 5 syncs automatically once wave 4 is Healthy.

```bash
kubectl get pods -n infisical -w
kubectl get pods -n infisical-operator-system -w
```

### 10b: Configure Infisical and create the Machine Identity

Open `https://infisical.local` and complete the one-time UI setup:

1. Create a user account and organisation (e.g. `ProperTea Local`)
2. Create a project (note its **slug**, e.g. `propertea`)
3. Add a secret: **key** = `masterkey`, **value** = `$(openssl rand -base64 32)`, **env** = `local`, **path** = `/`
4. Go to **Organisation Settings → Machine Identities → Create Identity**
   - Auth method: **Universal Auth**
   - Scope: the project above, role `Member` (or a custom read-only role)
5. After creation, select the identity and **Generate Credentials** — copy the **Client ID** and **Client Secret** before closing (they are shown once)

### 10c: Update the machine identity credential secret

The placeholder at `deploy/environments/local/zitadel/infisical-machine-identity.enc.yaml` is SOPS-encrypted. Replace the placeholder values in-place:

```bash
# Opens the decrypted YAML in $EDITOR, saves re-encrypted on exit
sops deploy/environments/local/zitadel/infisical-machine-identity.enc.yaml
```

Update `clientId` and `clientSecret` to the real values from step 10b.

### 10d: Update the projectSlug in the InfisicalSecret CR

Edit `deploy/environments/local/zitadel/infisical-secret.yaml` and replace `PLACEHOLDER_PROJECT_SLUG` with the Infisical project slug from step 10b.

### 10e: Commit and push

```bash
git add deploy/environments/local/zitadel/
git commit -m "chore(local): configure Infisical machine identity and project slug"
git push
```

ArgoCD detects the push. Wave 6 (`zitadel-config`) re-syncs:
- CNPG `Cluster` CR provisions the `zitadel-db` Postgres cluster (creates `zitadel-db-app`, `zitadel-db-superuser` Secrets automatically)
- `InfisicalSecret` CR prompts the operator to pull `masterkey` from Infisical → creates the `zitadel-masterkey` K8s Secret

Wave 7 (`zitadel`) then auto-syncs and finds the masterkey ready.

### What Infisical does NOT manage

CloudNativePG creates `zitadel-db-app` and `zitadel-db-superuser` automatically when the CNPG `Cluster` CR is applied. These are internal cluster credentials managed entirely by CNPG — no vault involvement needed. If you rebuild the cluster, CNPG recreates them and ZITADEL picks up the new credentials on first init.

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
  ├── zitadel-config.yaml      ← wave 6: CNPG Cluster + InfisicalSecret CR (creates zitadel-masterkey)
  └── zitadel.yaml             ← wave 7: ZITADEL Helm install + HTTPRoute (auto-sync, masterkey from operator)
```

Each cluster environment runs its own independent ArgoCD instance, with its own root app pointing only at its environment's apps. `deploy/infrastructure/` is a shared values library referenced by Applications via the `$values` pattern — it contains no Applications itself.

To update ArgoCD config after bootstrap:
- Base settings (SOPS CMP sidecar, insecure mode): `deploy/infrastructure/base/argocd/values.yaml`
- Local overrides (domain): `deploy/environments/local/argocd/values.yaml`

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
192.168.50.200 zitadel.local
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
      zitadel-config.yaml
      zitadel.yaml
    argocd/
      values.yaml               # ArgoCD env-specific overrides (domain: argocd.local)
      kustomization.yaml        # Patches base HTTPRoute hostname
      httproute-patch.yaml
    longhorn/
      values.yaml               # Longhorn env-specific overrides (replicas: 2, default StorageClass: true)
      kustomization.yaml
      httproute-patch.yaml
    zitadel/
      values.yaml               # ZITADEL env-specific overrides (ExternalDomain, DB host, seed user)
      kustomization.yaml
      httproute-patch.yaml
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
      zitadel/
        kustomization.yaml
        postgres-cluster.yaml   # CloudNativePG Cluster (canonical)
        values.yaml             # ZITADEL base Helm values (DB structure, OIDC, SAML)
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

## Extra
- Forward: 
POD=$(kubectl get pod -n argocd -l app.kubernetes.io/name=argocd-server -o jsonpath='{.items[0].metadata.name}')
kubectl port-forward pod/$POD -n argocd 8080:8080 --address 0.0.0.0 &
