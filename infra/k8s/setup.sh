#!/usr/bin/env bash
# Spins up a local kind cluster and deploys the full stack.
# Run from any directory; paths are resolved relative to this script.
set -euo pipefail

CLUSTER_NAME=dotnet-microsvcs-example
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "==> Creating kind cluster '${CLUSTER_NAME}'..."
if kind get clusters | grep -q "^${CLUSTER_NAME}$"; then
  echo "    Cluster already exists, skipping."
else
  kind create cluster --name "$CLUSTER_NAME"
fi

echo "==> Building service images..."
docker build -t order-api:latest "$ROOT_DIR/order-api"
docker build -t inventory:latest "$ROOT_DIR/inventory"

echo "==> Loading images into kind..."
kind load docker-image order-api:latest --name "$CLUSTER_NAME"
kind load docker-image inventory:latest --name "$CLUSTER_NAME"

echo "==> Applying manifests..."
kubectl apply -k "$SCRIPT_DIR"

echo ""
echo "Cluster is up. Run ./infra/k8s/wait-ready.sh to wait for all services and start port-forwarding."
