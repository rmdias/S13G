# S13G — Presentation

---

## Slide 1 — Title

# S13G: Brazilian Fiscal Document Processing System

**Event-driven backend for NFe, CTe, and NFSe XML documents**

- ASP.NET Core · .NET 10
- PostgreSQL · RabbitMQ · Docker
- Clean Architecture · CQRS · REST API

> Live: https://s13g.onrender.com/swagger

---

## Slide 2 — Problem

# The Problem

Brazilian companies are legally required to receive, store, and process government-issued fiscal XML documents.

| Challenge | Impact |
|---|---|
| Documents arrive in bulk | Processing must be fast and reliable |
| No built-in deduplication | Same document ingested multiple times |
| Strict schema rules (SEFAZ) | Invalid documents must be rejected early |
| Documents must be queryable | Raw XML alone is not enough |

**Manual processing is error-prone, slow, and unscalable.**

---

## Slide 3 — Solution Overview

# Solution Overview

A REST API that handles the full lifecycle of fiscal documents.

- **Upload** — accepts XML via multipart form
- **Validate** — checks structure and document type
- **Parse** — extracts metadata (CNPJ, state, value, date)
- **Deduplicate** — idempotency key prevents double ingestion
- **Persist** — stores document in PostgreSQL
- **Notify** — publishes event to RabbitMQ for async consumers

One endpoint handles ingestion. The rest of the system reacts to events.

---

## Slide 4 — Architecture

# Architecture

```
┌─────────────────────────────────┐
│           API Layer             │  ASP.NET Core · Swagger · FluentValidation
└─────────────────┬───────────────┘
                  │
┌─────────────────▼───────────────┐
│       Application Layer         │  CQRS · MediatR · Commands · Queries
└─────────────────┬───────────────┘
                  │
┌─────────────────▼───────────────┐
│         Domain Layer            │  Entities · Interfaces · Events
└─────────────────┬───────────────┘
                  │
┌─────────────────▼───────────────┐
│      Infrastructure Layer       │  PostgreSQL (EF Core) · RabbitMQ · XML Parser
└─────────────────────────────────┘
```

**Dependency rule:** outer layers depend on inner layers — never the reverse.

---

## Slide 5 — Event-Driven Flow

# Event-Driven Flow

```
Client
  │
  ▼
POST /documents/upload
  │
  ├─► XML validation (structure + root element)
  │
  ├─► Parse fields (CNPJ, state, value, date)
  │
  ├─► Dedup check (SHA-256 key → DocumentKeys table)
  │        │
  │        └─ duplicate? → return existing document
  │
  ├─► Persist FiscalDocument (PostgreSQL)
  │
  ├─► Publish DocumentProcessedEvent
  │         └─► RabbitMQ fanout exchange: "documents"
  │                    │
  │              queue: documents.processed
  │                    │
  │              RabbitMqConsumer (BackgroundService)
  │                    │
  └──────────────────► Write DocumentSummary (fast queries)

  ✗ failures → dead-letter exchange: documents.dlx
```

---

## Slide 6 — Key Technical Decisions

# Key Technical Decisions

| Decision | Reason |
|---|---|
| PostgreSQL over NoSQL | Relational queries, ACID transactions |
| SHA-256 idempotency key | O(1) dedup, no full-scan needed |
| DocumentSummary read-model | List queries skip raw XML blobs |
| Polly exponential backoff | Transient failures retried automatically |
| Publisher confirms | Guaranteed broker acknowledgement |
| Dead-letter exchange | Failed messages archived, not lost |
| EF Core migrations at startup | Schema always in sync on deploy |

---

## Slide 7 — API Endpoints

# API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/documents/upload` | Upload XML (multipart); `Idempotency-Key` header |
| `GET` | `/documents` | Paginated list; filters: CNPJ, state, date range |
| `GET` | `/documents/{id}` | Single document + raw XML |
| `PUT` | `/documents/{id}` | Update state / status |
| `DELETE` | `/documents/{id}` | Permanent delete |
| `GET` | `/health` | PostgreSQL + RabbitMQ status |

**Document types accepted:** NFe · CTe · NFSe

Invalid XML returns `400` with a structured error list — not a generic 500.

---

## Slide 8 — Testing Strategy

# Testing Strategy

### Unit Tests — NUnit · Moq · FluentAssertions

- XML parser: field extraction per document type
- Ingest handler: happy path + idempotency
- Query handlers: filters, pagination, not-found
- Idempotency: duplicate key → same document ID

### Integration Tests — Testcontainers

- Real PostgreSQL + RabbitMQ per test run
- POST XML → assert 201 + DB row + queue message
- Duplicate key → same ID, one DB row
- Invalid XML → 400 + error list

### CI — GitHub Actions

- Runs on every push and pull request
- Build → unit tests → integration tests (Docker required)

---

## Slide 9 — Live Deployment

# Live Deployment

```
Push to main
     │
     ▼
GitHub Actions: CI
  build → unit tests → integration tests
     │
     ▼ (green)
GitHub Actions: CD
  POST → Render deploy hook
     │
     ▼
Render: build Docker image → run container
     │
     ▼
EF Core migrations run on startup
     │
     ▼
GET /health passes → deploy complete
```

| Service | Provider | Plan |
|---|---|---|
| API | Render | Free |
| PostgreSQL | Render | Free |
| RabbitMQ | CloudAMQP | Little Lemur (free) |

> **Live:** https://s13g.onrender.com/swagger
> First request after 15 min idle: ~30–60s cold start

---

## Slide 10 — What Could Be Next

# What Could Be Next

| Area | Improvement |
|---|---|
| **Security** | JWT / API-key authentication |
| **Storage** | Move raw XML to S3/Azure Blob; store reference URL |
| **Validation** | Full SEFAZ XSD schema validation |
| **Reliability** | Outbox pattern — eliminate publish/persist race |
| **Scalability** | Extract consumer to independent worker service |
| **Observability** | OpenTelemetry · Serilog · Prometheus/Grafana |
| **Audit** | Soft delete — preserve history with `DeletedAt` |

These are known trade-offs accepted for a demo scope.
The architecture supports all of them without layer changes.
