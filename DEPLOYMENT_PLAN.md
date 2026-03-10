# Deployment Plan — S13G

## Target Architecture

```
GitHub (source + CI/CD)
    │
    ├── GitHub Actions CI ──► build + unit tests + integration tests (every push/PR)
    │
    └── GitHub Actions CD ──► trigger Render deploy (main only, after CI passes)
                                        │
                              ┌─────────┴──────────┐
                              ▼                    ▼
                    Render Web Service     Render PostgreSQL
                    (.NET 10 API)          (managed, same region)
                              │
                              └──────────────────────►  CloudAMQP
                                                        (managed RabbitMQ)
```

**No infrastructure to own or operate.** All three backing services are fully managed.

---

## Services Overview

| Service | Provider | Plan | Purpose |
|---|---|---|---|
| Web API | Render | Free | Runs the .NET 10 ASP.NET Core app |
| PostgreSQL | Render | Free | Persistent storage (deleted after 90 days) |
| RabbitMQ | CloudAMQP | Little Lemur (free) | Async messaging, document events |
| Container Registry | GitHub (GHCR) | Free | Stores Docker images |
| CI/CD | GitHub Actions | Free (public repo) | Build, test, deploy pipeline |

**Estimated monthly cost: $0** — all free tiers.

### Free tier limitations (demo use — acceptable)

| Limitation | Impact |
|---|---|
| Web service spins down after 15 min of inactivity | First request after idle takes ~30–60s (cold start) |
| PostgreSQL deleted after 90 days | Data loss after 90 days — recreate the DB or upgrade to paid if needed |
| CloudAMQP limited to 1M messages/month | More than enough for a demo |

---

## Action Points

### Phase 1 — Local foundations (do first, no accounts needed)

| # | Action | Priority | Complexity | Files |
|---|---|---|---|---|
| 1.1 | Write multi-stage `Dockerfile` | P0 | Low | `Dockerfile` |
| 1.2 | Add `.dockerignore` | P0 | Low | `.dockerignore` |
| 1.3 | Add `appsettings.Production.json` skeleton (no secrets, only structure) | P0 | Low | `src/Api/appsettings.Production.json` |
| 1.4 | Verify app reads config from environment variables (connection string, RabbitMQ) | P0 | Low | `src/Api/Program.cs` |

---

### Phase 2 — GitHub Actions CI

| # | Action | Priority | Complexity | Files |
|---|---|---|---|---|
| 2.1 | Create `ci.yml` — build + unit tests on every push and PR | P0 | Low | `.github/workflows/ci.yml` |
| 2.2 | Add integration tests step to `ci.yml` (Testcontainers, requires Docker on runner) | P1 | Low | `.github/workflows/ci.yml` |
| 2.3 | Add `dependabot.yml` for NuGet and GitHub Actions version bumps | P2 | Low | `.github/dependabot.yml` |

---

### Phase 3 — External accounts setup

| # | Action | Priority | Complexity | Notes |
|---|---|---|---|---|
| 3.1 | Create [CloudAMQP](https://www.cloudamqp.com) account, create "Little Lemur" instance | P0 | Very low | Copy the `AMQP URL` — format: `amqp://user:pass@host/vhost` |
| 3.2 | Create [Render](https://render.com) account, connect GitHub repo | P0 | Very low | Required before creating services |
| 3.3 | Create Render PostgreSQL instance (same region as web service) | P0 | Very low | Copy the internal connection string |
| 3.4 | Create Render Web Service, select "Deploy from Docker image" or "Dockerfile" | P0 | Low | Set health check path to `/health` |

---

### Phase 4 — Secrets and environment variables

#### GitHub Secrets (Settings → Secrets → Actions)

| Secret name | Value |
|---|---|
| `RENDER_DEPLOY_HOOK_URL` | Deploy hook URL from Render web service settings |

#### Render Environment Variables (web service → Environment)

| Variable | Value | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ASPNETCORE_URLS` | `http://+:10000` | Render expects port 10000 by default |
| `ConnectionStrings__DefaultConnection` | Render internal DB connection string | Copy from Render PostgreSQL dashboard |
| `RabbitMq__HostName` | CloudAMQP hostname | From AMQP URL |
| `RabbitMq__UserName` | CloudAMQP username | From AMQP URL |
| `RabbitMq__Password` | CloudAMQP password | From AMQP URL |
| `RabbitMq__VirtualHost` | CloudAMQP vhost | From AMQP URL (usually a short string after the last `/`) |
| `RabbitMq__Port` | `5672` | |

> **Note:** ASP.NET Core maps `__` (double underscore) to nested JSON keys, so `RabbitMq__HostName` overrides `appsettings.json → RabbitMq → HostName`.

---

### Phase 5 — GitHub Actions CD

| # | Action | Priority | Complexity | Files |
|---|---|---|---|---|
| 5.1 | Create `cd.yml` — triggers Render deploy hook after CI passes on `main` | P0 | Low | `.github/workflows/cd.yml` |
| 5.2 | Add post-deploy health check step (curl `/health`, fail pipeline if down) | P1 | Low | `.github/workflows/cd.yml` |

---

### Phase 6 — Hardening (after first successful deploy)

| # | Action | Priority | Complexity | Notes |
|---|---|---|---|---|
| 6.1 | Add GitHub Environment `production` with required-reviewer approval gate | P2 | Low | Prevents accidental deploys to prod |
| 6.2 | Pin Render to a specific Docker image tag (not `latest`) for predictable rollbacks | P2 | Low | Pass Git SHA as image tag |
| 6.3 | Upgrade Render PostgreSQL to paid plan before 90-day expiry if data needs to persist | P3 | Very low | Free DB is deleted at 90 days |
| 6.4 | Add Sentry or similar for error tracking | P3 | Low | Optional — useful once live |

---

## Deployment Flow (once all phases are done)

```
Developer pushes to main
        │
        ▼
GitHub Actions: ci.yml
  ├── dotnet build
  ├── dotnet test (unit)
  └── dotnet test (integration) [Testcontainers]
        │
        ▼ (all green)
GitHub Actions: cd.yml
  └── POST to Render deploy hook URL
        │
        ▼
Render pulls latest image from GHCR
  └── runs new container
        │
        ▼
EF Core migrations run on startup
        │
        ▼
/health check passes → deploy complete
```

---

## Rollback

Render keeps previous deploys available. To roll back:
- Go to Render dashboard → web service → **Deploys** → click any previous deploy → **Redeploy**.

For a fully automated rollback (Phase 6), the CD workflow can re-trigger the previous image tag if the health check fails.

---

## Recommended execution order

1. Phase 1 (Dockerfile + config) — all local, no accounts
2. Phase 2.1 (CI with unit tests) — get the green check on GitHub first
3. Phase 3 (create accounts)
4. Phase 4 (configure secrets and env vars)
5. Phase 2.2 (add integration tests to CI)
6. Phase 5 (CD workflow)
7. Phase 6 (hardening)
