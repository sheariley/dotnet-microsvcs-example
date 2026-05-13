# Kubernetes Deployment

Runs the same stack as `docker-compose.yml` on a local [kind](https://kind.sigs.k8s.io/) cluster.

## Quick start

```bash
# 1. Create cluster, build images, apply manifests
chmod +x infra/k8s/setup.sh
./infra/k8s/setup.sh

# 2. Wait for all services, then start port-forwards (Ctrl+C to stop)
chmod +x infra/k8s/wait-ready.sh
./infra/k8s/wait-ready.sh
```

Then start the React dev server (`npm run dev` inside `web/`). The app connects to `order-api` on `localhost:5001` as usual. Open the Aspire Dashboard at `http://localhost:18888` to verify traces are flowing end-to-end.

## Teardown

```bash
kind delete cluster --name dotnet-microsvcs-example
```

## Structure

```
infra/k8s/
  setup.sh                   # creates cluster, builds/loads images, applies manifests
  wait-ready.sh              # waits for all rollouts, then starts port-forwards
  kustomization.yaml         # kubectl apply -k entry point
  configmap.yaml             # shared env vars (OTel endpoint, Kafka bootstrap)
  postgres/
    pvc.yaml                 # 2 GiB persistent volume claim
    deployment.yaml
    service.yaml
  kafka/
    statefulset.yaml         # single-broker KRaft StatefulSet (confluentinc/cp-kafka:7.9.0)
    service.yaml             # ClusterIP for client connections (kafka:9092)
    service-headless.yaml    # headless Service required by StatefulSet for pod DNS
  order-api/
    deployment.yaml
    service.yaml
  inventory/
    deployment.yaml
    service.yaml
  aspire-dashboard/
    deployment.yaml
    service.yaml             # exposes :18888 (UI) and :18889 (OTLP gRPC)
```

## Compose → Kubernetes mapping

| Docker Compose concept | Kubernetes equivalent | Notes |
|---|---|---|
| `services.order-api` | `Deployment` + `Service` | Deployment manages pod replicas; Service gives it a stable DNS name |
| `environment:` | `ConfigMap` via `envFrom` | Shared vars in `configmap.yaml`; service-specific vars inline in the Deployment |
| `ports: "5001:8080"` | `kubectl port-forward` | No Ingress for this dev setup; port-forward maps a cluster Service to localhost |
| `depends_on:` | Readiness probes | Kubernetes retries until the probe passes rather than delaying startup |
| `volumes:` named volume | `PersistentVolumeClaim` | PVC is claimed by the Deployment; data survives pod restarts |
| `kafka:` service | `StatefulSet` + two `Service`s | Same `confluentinc/cp-kafka` image as Compose; headless Service for pod DNS, ClusterIP Service for client connections at `kafka:9092` |

## Why port-forward instead of Ingress

An Ingress controller (e.g. ingress-nginx) requires an additional Helm install and a `LoadBalancer` or `NodePort` service to expose it on the host. For a local dev cluster the extra ceremony isn't worth it — `kubectl port-forward` maps a cluster Service directly to a localhost port with one command and no additional infrastructure.

## Note on Postgres

A single Postgres instance hosts two logical databases: `order_api` (owned by `order-api`) and `inventory` (owned by `inventory`). Each service connects to its own database and runs EF Core migrations on startup to create it if it doesn't exist. In a production deployment these would be separate Postgres instances so that schema migrations and scaling decisions are fully independent per service.
