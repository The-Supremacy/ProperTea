#!/usr/bin/env bash
# Installs the base cluster infrastructure after bootstrap:
#   1. Gateway API CRDs
#   2. Cilium (CNI, kube-proxy replacement, Hubble, Gateway API, L2)
#   3. Local Path Provisioner (bootstrap-only default StorageClass)
#      Longhorn is the persistent default StorageClass, but it arrives later
#      via GitOps. Local Path Provisioner covers the window between ArgoCD
#      install and Longhorn becoming ready (e.g. ArgoCD's own Redis PVC).
#   4. kubelet-csr-approver
#
# Run from the repo root or deploy/environments/local/cluster.
# Prerequisites: kubectl and helm configured and pointing at the cluster.
#
# Usage:
#   ./scripts/install-infrastructure.sh

set -euo pipefail

CILIUM_VERSION="1.19.1"
GATEWAY_API_VERSION="v1.4.1"
CP_IP="192.168.50.10"

echo "=== Step 1: Gateway API CRDs ==="
# Use the experimental bundle — it includes all standard CRDs plus experimental ones
# (ReferenceGrant, GRPCRoute, TLSRoute, BackendLBPolicy, etc.) that Cilium requires.
# Using individual standard files risks missing CRDs that Cilium's operator checks for.
kubectl apply -f "https://github.com/kubernetes-sigs/gateway-api/releases/download/${GATEWAY_API_VERSION}/experimental-install.yaml" --server-side
echo "Gateway API CRDs installed."

echo ""
echo "=== Step 2: Cilium ${CILIUM_VERSION} ==="
helm repo add cilium https://helm.cilium.io/ 2>/dev/null || true
helm repo update cilium

# Talos-specific settings:
#   cgroup.autoMount.enabled=false  -- Talos manages cgroups; remounting is not permitted
#   cgroup.hostRoot                 -- tell Cilium where Talos mounts the cgroup fs
#   securityContext.capabilities.*  -- Talos restricts capability application; must be explicit
# L7 gateway is handled by Envoy Gateway (installed via ArgoCD at sync-wave 2).
# Cilium is CNI + kube-proxy replacement + L2 load balancer only.
# gatewayAPI.enabled is intentionally absent -- we do not want the cilium GatewayClass.
helm install cilium cilium/cilium \
  --version "$CILIUM_VERSION" \
  --namespace kube-system \
  --set ipam.mode=kubernetes \
  --set kubeProxyReplacement=true \
  --set k8sServiceHost="$CP_IP" \
  --set k8sServicePort=6443 \
  --set securityContext.capabilities.ciliumAgent="{CHOWN,KILL,NET_ADMIN,NET_RAW,IPC_LOCK,SYS_ADMIN,SYS_RESOURCE,DAC_OVERRIDE,FOWNER,SETGID,SETUID}" \
  --set securityContext.capabilities.cleanCiliumState="{NET_ADMIN,SYS_ADMIN,SYS_RESOURCE}" \
  --set cgroup.autoMount.enabled=false \
  --set cgroup.hostRoot=/sys/fs/cgroup \
  --set hubble.enabled=true \
  --set hubble.relay.enabled=true \
  --set hubble.ui.enabled=true \
  --set l2announcements.enabled=true \
  --set prometheus.enabled=true \
  --set operator.prometheus.enabled=true \
  --server-side \
  --set devices=ens3

echo "Waiting for Cilium agents to become ready..."
kubectl -n kube-system wait \
  --for=condition=Ready pod \
  -l app.kubernetes.io/name=cilium-agent \
  --timeout=180s

echo ""
kubectl get nodes -o wide

echo ""
echo "=== Step 3: Local Path Provisioner ==="
kubectl apply -f https://raw.githubusercontent.com/rancher/local-path-provisioner/master/deploy/local-path-storage.yaml --server-side
kubectl patch storageclass local-path \
  -p '{"metadata": {"annotations":{"storageclass.kubernetes.io/is-default-class":"true"}}}'

# Talos enforces baseline PSA cluster-wide by default. The local-path provisioner
# creates helper pods with hostPath volumes in its own namespace, which baseline blocks.
# Label the namespace privileged so PVC provisioning works.
kubectl label namespace local-path-storage \
  pod-security.kubernetes.io/enforce=privileged \
  pod-security.kubernetes.io/warn=privileged \
  pod-security.kubernetes.io/audit=privileged \
  --overwrite
echo "local-path set as default StorageClass."

echo ""
echo "=== Step 4: kubelet-csr-approver ==="
# Talos uses rotate-server-certificates=true, which causes kubelets to request
# serving cert renewals via CSR. Without an approver, kubectl logs/exec break
# after the first cert rotation because the new serving cert is never approved.
helm repo add kubelet-csr-approver https://postfinance.github.io/kubelet-csr-approver 2>/dev/null || true
helm repo update kubelet-csr-approver 2>/dev/null

helm install kubelet-csr-approver kubelet-csr-approver/kubelet-csr-approver \
  --namespace kube-system \
  --set providerRegex='^talos-.+$' \
  --server-side \
  --set bypassDnsResolution=true
echo "kubelet-csr-approver installed."

echo ""
echo "=== Done ==="
kubectl get nodes -o wide
kubectl get storageclass
echo ""
echo "All nodes should be Ready. Next: run install-argocd.sh"
