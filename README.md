# Order Processing Microservices

A small event-driven order processing system built with .NET, Apache Kafka, PostgreSQL, React, and WebSockets.

## Architecture

```
React UI  ──HTTP──▶  order-api  ──Kafka──▶  inventory
          ◀──WS────  (.NET/PG)              (.NET/PG)
```

Two .NET services communicate asynchronously through a single Kafka topic. The `order-api` service accepts orders over HTTP, publishes events to Kafka, and pushes live status updates to the browser via WebSockets. The `inventory` service consumes those events, attempts stock reservation, and publishes the outcome back. Distributed traces, metrics, and logs flow through OpenTelemetry to the Aspire Dashboard.

## Stack

| Layer | Technology |
|---|---|
| REST API + WebSocket server | ASP.NET Core (.NET 10), PostgreSQL |
| Inventory processor | ASP.NET Core (.NET 10), PostgreSQL |
| Message broker | Apache Kafka (KRaft mode, single broker) |
| Front-end | React + Vite |
| Observability | OpenTelemetry → Aspire Dashboard |
| Local orchestration | Docker Compose |
| Cloud orchestration | Kubernetes (kind) |

## Running locally

**Prerequisites:** Docker with Compose, Node.js 18+.

```bash
cp .env.example .env
docker compose up --build
```

| Endpoint | URL |
|---|---|
| order-api | http://localhost:5001 |
| inventory | http://localhost:5002 |
| Aspire Dashboard | http://localhost:18888 |

React dev server (separate terminal):

```bash
cd web && npm install && npm run dev
# → http://localhost:5173
```

## Authentication note

This project uses a mock customer-picker in place of real authentication. Clicking a seeded customer stores a `customerId` in `sessionStorage`; that ID is sent on every API call and WebSocket connection. There are no passwords, tokens, or sessions. See `order-api` for details.
