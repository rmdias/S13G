# S13G — Quick Start

## Option A: Docker Compose (Recommended)

Requires Docker running on your machine.

```bash
# Start PostgreSQL and RabbitMQ containers, restore packages, build, and apply migrations
bash scripts/setup.sh

# Then start the API
cd src/Api && dotnet run
```

---

## Option B: Local Installation (macOS with Homebrew)

Installs PostgreSQL and RabbitMQ via Homebrew, creates the database, and applies migrations.

```bash
bash scripts/setup-local-macos.sh

# Then start the API
cd src/Api && dotnet run
```

**Manage services:**
```bash
brew services start postgresql@15
brew services start rabbitmq
brew services stop postgresql@15
brew services stop rabbitmq
```

---

## Option C: Local Installation (Linux)

```bash
sudo bash scripts/setup-local-linux.sh

cd src/Api && dotnet run
```

**Manage services:**
```bash
sudo systemctl start postgresql
sudo systemctl start rabbitmq-server
```

---

## Option D: Local Installation (Windows)

Run PowerShell as Administrator:

```powershell
.\scripts\setup-local-windows.ps1
```

Then start the API:
```bash
cd src/Api && dotnet run
```

---

## After Setup: API Endpoints

- **HTTP**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **HTTPS**: https://localhost:5001 (requires `dotnet dev-certs https --trust`)

---

## Smoke Test

```bash
# Returns OpenAPI spec — works without database
curl http://localhost:5000/swagger/v1/swagger.json

# Open Swagger UI (macOS)
open http://localhost:5000/swagger

# Open Swagger UI (Windows)
start http://localhost:5000/swagger
```

---

## External Services — Default Settings

### PostgreSQL

| Setting | Value |
|---|---|
| Host | localhost:5432 |
| Database | s13g |
| Username | postgres |
| Password | postgres |

Override credentials at runtime without editing `appsettings.json`:

```bash
export DB_USER=myuser
export DB_PASS=mypassword
cd src/Api && dotnet run
```

> On macOS Homebrew installations, Postgres defaults to the current OS user with no password.
> `setup-local-macos.sh` handles this automatically by setting `DB_USER=$(whoami)`.

### RabbitMQ

| Setting | Value |
|---|---|
| Host | localhost:5672 |
| Username | guest |
| Password | guest |
| Exchange | `documents` |
| Queue | `documents.processed` |
| Dead-letter exchange | `documents.dlx` |
| Management UI | http://localhost:15672 (guest/guest) |

---

## What Works Without External Services

| Capability | Requires |
|---|---|
| Swagger UI | Nothing |
| REST API routing | Nothing |
| Document upload | PostgreSQL + RabbitMQ |
| Document list / get | PostgreSQL |
| RabbitMQ consumer | RabbitMQ |

Database errors and RabbitMQ failures are logged and handled gracefully — the API will not crash.

---

## Project Information

- **Framework**: .NET 10.0
- **API Port**: 5000 (HTTP) / 5001 (HTTPS)
- **Database**: PostgreSQL (configured in `src/Api/appsettings.json`)
- **Message Queue**: RabbitMQ (configured in `src/Api/appsettings.json`)
- **Documentation**: [README.md](README.md) — full setup | [ARCHITECTURE.md](ARCHITECTURE.md) — design docs
