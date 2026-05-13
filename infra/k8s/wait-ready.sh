#!/usr/bin/env bash
set -euo pipefail

echo "==> Waiting for services to become ready..."
kubectl rollout status statefulset/kafka
kubectl rollout status deployment/postgres
kubectl rollout status deployment/aspire-dashboard
kubectl rollout status deployment/order-api
kubectl rollout status deployment/inventory

echo ""
echo "All services are up. Starting port-forwards..."
echo ""

kubectl port-forward svc/order-api 5001:8080 &
kubectl port-forward svc/inventory 5002:8080 &
kubectl port-forward svc/aspire-dashboard 18888:18888 &

sleep 2

echo ""
echo "┌---------------------------------------------┐"
echo "|  order-api        → http://localhost:5001   |"
echo "|  inventory        → http://localhost:5002   |"
echo "|  aspire-dashboard → http://localhost:18888  |"
echo "└---------------------------------------------┘"
echo "Press Ctrl+C to stop."

wait
