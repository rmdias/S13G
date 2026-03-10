# Deployment Reference — S13G

## Architecture

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

## Services

| Service | Provider | Plan |
|---|---|---|
| Web API | Render | Free |
| PostgreSQL | Render | Free |
| RabbitMQ | CloudAMQP | Little Lemur (free) |
| CI/CD | GitHub Actions | Free (public repo) |

### Free tier limitations

| Limitation | Impact |
|---|---|
| Web service spins down after 15 min of inactivity | First request after idle takes ~30–60s (cold start) |
| PostgreSQL deleted after 90 days | Recreate the DB or upgrade to paid if data needs to persist |
| CloudAMQP limited to 1M messages/month | Sufficient for demo use |

---

## Environment Variables (Render web service)

| Variable | Value | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ASPNETCORE_URLS` | `http://+:10000` | Render expects port 10000 by default |
| `ConnectionStrings__DefaultConnection` | Render internal DB connection string | Copy from Render PostgreSQL dashboard — URI format is accepted |
| `RabbitMq__HostName` | CloudAMQP hostname | From AMQP URL |
| `RabbitMq__UserName` | CloudAMQP username | From AMQP URL |
| `RabbitMq__Password` | CloudAMQP password | From AMQP URL |
| `RabbitMq__VirtualHost` | CloudAMQP vhost | Last path segment of the AMQP URL |
| `RabbitMq__Port` | `5672` | |

> ASP.NET Core maps `__` (double underscore) to nested JSON keys, so `RabbitMq__HostName` overrides `appsettings.json → RabbitMq → HostName`.

### GitHub Secrets (Settings → Secrets → Actions)

| Secret | Value |
|---|---|
| `RENDER_DEPLOY_HOOK_URL` | Deploy hook URL from Render web service settings |

---

## Deployment Flow

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
Render builds Docker image and runs new container
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
