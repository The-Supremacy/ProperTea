#!/usr/bin/env bash
# Sets up persistent port forwarding from the infra VM's external interface
# to the Talos control plane node (192.168.50.10).
#
# This allows the dev VM to reach talosctl (port 50000) and kubectl (port 6443)
# via the infra VM's Default Switch IP.
#
# Run on propertea-infra. Installs a systemd service for persistence.
#
# Usage:
#   sudo ./setup-port-forwards.sh

set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "Error: run with sudo"
  exit 1
fi

apt install -y socat

# Create systemd service for port forwarding
cat > /etc/systemd/system/talos-port-forward.service << 'EOF'
[Unit]
Description=Port forwards for Talos cluster access
After=network.target libvirtd.service
Wants=libvirtd.service

[Service]
Type=forking
ExecStart=/bin/bash -c '\
  socat TCP-LISTEN:50000,fork,reuseaddr TCP:192.168.50.10:50000 & \
  socat TCP-LISTEN:6443,fork,reuseaddr TCP:192.168.50.10:6443 & \
  wait'
ExecStop=/usr/bin/pkill -f "socat TCP-LISTEN:(50000|6443)"
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now talos-port-forward.service

echo ""
echo "Port forwards active:"
echo "  :50000 -> 192.168.50.10:50000 (talosctl API)"
echo "  :6443  -> 192.168.50.10:6443  (Kubernetes API)"
echo ""
echo "From the dev VM, use the infra VM's IP as the endpoint."
