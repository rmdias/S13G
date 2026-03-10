# S13G - Brazilian Fiscal Documents Processing System

A robust, event-driven backend system built with ASP.NET Core for processing, storing, and managing Brazilian fiscal XML documents (NFe, CTe, NFSe). The system implements Clean Architecture principles with CQRS patterns, ensuring scalability, maintainability, and reliable event-driven workflows.

## 🌐 Live Environment

The API is deployed and running at:

| Resource | URL |
|---|---|
| API Base | https://s13g.onrender.com |
| Swagger UI | https://s13g.onrender.com/swagger |
| Health Check | https://s13g.onrender.com/health |

**Infrastructure:**
- **API**: Render (free tier — cold starts after 15 min of inactivity)
- **Database**: Render PostgreSQL (free tier)
- **Message Broker**: CloudAMQP — Little Lemur (free tier)

> First request after a period of inactivity may take ~30–60s due to the free tier cold start.

---

## 🚀 Features

- **Document Ingestion**: Upload and process Brazilian fiscal XML documents with automatic validation
- **Event-Driven Architecture**: Asynchronous processing with RabbitMQ messaging
- **Idempotent Operations**: Prevents duplicate document processing using idempotency keys
- **RESTful API**: Comprehensive REST API with OpenAPI/Swagger documentation
- **Database Persistence**: PostgreSQL with Entity Framework Core for reliable data storage
- **Comprehensive Testing**: Unit and integration tests with Testcontainers
- **Clean Architecture**: Separation of concerns with Domain, Application, Infrastructure, and API layers

## 🏗️ Architecture

The system follows Clean/Onion Architecture with vertical slices:

```
┌─────────────────────┐
│   ASP.NET Core API  │
│   + Swagger UI      │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Application Layer   │
│ - CQRS (MediatR)    │
│ - FluentValidation  │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Domain Layer        │
│ - Entities          │
│ - Business Rules    │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Infrastructure      │
│ - PostgreSQL (EF)   │
│ - RabbitMQ          │
│ - XML Parser        │
└─────────────────────┘
```

### Key Components

- **API Layer**: REST controllers with automatic request validation
- **Application Layer**: Command/Query handlers using MediatR
- **Domain Layer**: Core business entities and domain events
- **Infrastructure Layer**: External concerns (database, messaging, XML processing)

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [RabbitMQ 3.12+](https://www.rabbitmq.com/download.html)
- [Docker](https://www.docker.com/) (for running tests with Testcontainers)

## 🛠️ Installation & Setup

### Choose Your Setup Method

Choose one of these three options:

---

## **Option A: Docker Compose (Recommended - No Dependencies to Install)**

Perfect if you have Docker installed. Services run in isolated containers.

```bash
bash scripts/setup.sh
```

This will:
- ✅ Check Docker and Docker Compose
- ✅ Start PostgreSQL and RabbitMQ in containers
- ✅ Wait for services to be healthy
- ✅ Restore NuGet packages
- ✅ Build the solution
- ✅ Apply database migrations

Then start the API:
```bash
cd src/Api && dotnet run
```

**Requires:** Docker & Docker Compose

> A `Dockerfile` at the repository root is also used for production deployments
> (e.g. Render). It targets port `10000` (`ASPNETCORE_URLS=http://+:10000`) which
> is the default port expected by Render's platform. Local `dotnet run` still uses
> port `5000` via `launchSettings.json`.

---

## **Option B: Local Installation**

Install PostgreSQL and RabbitMQ directly on your machine. Choose your OS:

### **macOS (with Homebrew)**

```bash
bash scripts/setup-local-macos.sh
```

Then start the API:
```bash
cd src/Api
dotnet run
```

**What it does:**
- ✅ Installs PostgreSQL 15 via Homebrew
- ✅ Installs RabbitMQ via Homebrew
- ✅ Starts both services
- ✅ Creates the database
- ✅ Applies migrations

**Requires:** Homebrew

**Manage services:**
```bash
brew services start postgresql@15      # Start DB
brew services start rabbitmq           # Start RabbitMQ
brew services stop postgresql@15       # Stop DB
brew services stop rabbitmq            # Stop RabbitMQ
```

---

### **Linux (Ubuntu/Debian)**

```bash
sudo bash scripts/setup-local-linux.sh
```

Then start the API:
```bash
cd src/Api
dotnet run
```

**What it does:**
- ✅ Installs PostgreSQL 15 via apt
- ✅ Installs RabbitMQ via apt
- ✅ Starts both services
- ✅ Creates the database
- ✅ Applies migrations

**Requires:** sudo access, apt-get

**Manage services:**
```bash
sudo systemctl start postgresql         # Start DB
sudo systemctl start rabbitmq-server    # Start RabbitMQ
sudo systemctl stop postgresql          # Stop DB
sudo systemctl stop rabbitmq-server     # Stop RabbitMQ
```

---

### **Windows (with Chocolatey)**

Run PowerShell as Administrator:

```powershell
.\scripts\setup-local-windows.ps1
```

Then start the API:
```bash
cd src/Api
dotnet run
```

**What it does:**
- ✅ Installs Chocolatey (if needed)
- ✅ Installs PostgreSQL 15
- ✅ Installs RabbitMQ
- ✅ Starts both services
- ✅ Creates the database
- ✅ Applies migrations

**Requires:** Administrator access

**Manage services:**
1. Open Services (services.msc)
2. Find PostgreSQL and RabbitMQ services
3. Right-click → Start/Stop

---

## **Option C: Manual Setup (Advanced)**

If scripts don't work for your setup, you can install everything manually:

#### 1. Clone the Repository

```bash
git clone https://github.com/your-username/S13G.git
cd S13G
```

#### 2. Install Services

**PostgreSQL:**
- Download from https://www.postgresql.org/download/
- Install PostgreSQL 15 or later
- Ensure it's running on port 5432

**RabbitMQ:**
- Download from https://www.rabbitmq.com/download.html
- Install RabbitMQ 3.12 or later
- Ensure it's running on port 5672

#### 3. Create Database

```bash
# On macOS/Linux
psql -U postgres -c "CREATE DATABASE s13g;"

# On Windows (in PostgreSQL Command Line)
CREATE DATABASE s13g;
```

#### 4. Update Connection Strings (if needed)

Edit `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=s13g;Username=postgres;Password=YOUR_PASSWORD"
  },
}
```

> **Tip:** the API also respects the `DB_USER` and `DB_PASS` environment variables
> to override the username and password without editing the file. On macOS Homebrew
> setups the database defaults to the current OS user with no password — set
> `DB_USER=$(whoami)` before running the API.
>
> For local development you can also create `src/Api/appsettings.Development.json`
> (gitignored) with your local connection string. `launchSettings.json` ensures
> `ASPNETCORE_ENVIRONMENT=Development` is set automatically when using `dotnet run`,
> so the development overrides are loaded without any extra env var configuration.
>
> The API also accepts Render-style PostgreSQL URIs (`postgresql://user:pass@host/db`)
> in `ConnectionStrings__DefaultConnection` — they are converted to Npgsql key=value
> format at startup.

#### 5. Apply Database Migrations

Migrations can be applied in one of two ways:

- **Recommended:** run from the `Infrastructure` project which includes the
  design-time factory. This avoids loading the full ASP.NET host and
  sidesteps DI/startup errors.

```bash
cd src/Infrastructure
# keeps existing build artifacts, faster
dotnet ef database update --no-build --project Infrastructure.csproj
```

- **Alternative:** run from the API project. If you do this you should either
  set the connection string environment variable (e.g. use `${{USER}}` as the
  database user on macOS/Homebrew setups) or ensure the `postgres` role exists
  in your server.

```bash
cd src/Api
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=s13g;Username=${USER};"
# option: specify password too if you configured one

# then run

```

If you get an error about Entity Framework tools, install them:
```bash
dotnet tool install --global dotnet-ef --version 9.0.3
```

#### 6. Build and Run

```bash
dotnet restore
dotnet build
cd src/Api
dotnet run
```

---

## Running the API

After setup is complete using any of the options above, start the API:

```bash
cd src/Api
dotnet run
```

The API will be available at:
- **HTTP**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

If you have a trusted development certificate installed the server also binds
HTTPS on port 5001. To set this up run:

```bash
# install and trust a dev cert (macOS will prompt for approval)
dotnet dev-certs https --trust
```

Then you can browse to `https://localhost:5001/swagger` as well.

## Configuration

All settings are in `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=s13g;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "documents",
    "QueueName": "documents.processed",
    "DeadLetterExchange": "documents.dlx",
    "RetryCount": 5,
    "RetryInitialDelayMs": 500
  }
}
```

Update these values if your services run on different hosts/ports.

## 📖 API Usage

### Endpoints

#### Upload Document
```http
POST /documents/upload
Content-Type: multipart/form-data

Form Data:
- file: [XML file]
- Idempotency-Key: [header] unique-key-for-request
```

#### List Documents
```http
GET /documents?page=1&pageSize=20&cnpj=12345678901234&uf=SP&fromDate=2024-01-01&toDate=2024-12-31
```

#### Get Document by ID
```http
GET /documents/{id}
```

#### Update Document
```http
PUT /documents/{id}
Content-Type: application/json

{
  "state": "SP",
  "status": "Processed"
}
```

#### Delete Document
```http
DELETE /documents/{id}
```

#### Health Check
```http
GET /health
```

Returns a JSON report with the status of `postgresql` and `rabbitmq` services:
```json
{
  "status": "Healthy",
  "services": {
    "postgresql": { "status": "Healthy", "error": null },
    "rabbitmq":   { "status": "Healthy", "error": null }
  }
}
```

### Document Types Supported

- **NFe**: Nota Fiscal Eletrônica (Electronic Invoice)
- **CTe**: Conhecimento de Transporte Eletrônico (Electronic Transport Knowledge)
- **NFSe**: Nota Fiscal de Serviços Eletrônica (Electronic Service Invoice)

## 🧪 Testing

### Unit Tests

```bash
cd tests/Unit
dotnet test
```

### Integration Tests

Integration tests use Testcontainers for PostgreSQL and RabbitMQ:

```bash
cd tests/Integration
dotnet test
```

Note: Integration tests require Docker to be running.

## 🔧 Development

### Project Structure

```
S13G/
├── src/
│   ├── Api/                 # ASP.NET Core Web API
│   ├── Application/         # CQRS handlers and services
│   ├── Domain/              # Entities, events, interfaces
│   └── Infrastructure/      # External dependencies
├── tests/
│   ├── Unit/                # Unit tests
│   └── Integration/         # Integration tests
├── ARCHITECTURE.md          # Detailed architecture docs
└── S13G.sln                # Solution file
```

### Key Technologies

- **ASP.NET Core (.NET 10)**: Web framework
- **Entity Framework Core**: ORM for PostgreSQL
- **MediatR**: CQRS implementation
- **FluentValidation**: Request validation
- **RabbitMQ.Client**: Message queuing
- **Polly**: Resilience policies
- **Swashbuckle**: OpenAPI documentation
- **NUnit**: Testing framework
- **Testcontainers**: Integration test containers

### Dependency Health

The project targets .NET 10 with several third‑party libraries. Occasionally packages ship with security advisories; you may see warnings during `dotnet build` such as:

```
warning NU1903: Package 'Npgsql' 8.0.0 has a known high severity vulnerability
```

To keep dependencies up-to-date:

1. Run `dotnet list package --vulnerable` to see packages with advisories.
2. Edit the appropriate `.csproj` file(s) and bump the `Version` attribute to a patched release.
3. Execute `dotnet restore` and rebuild.

The current repository has already been updated to use patched versions and includes explicit references where necessary; you can change them as new fixes are released.

## 🔒 Sensitive Data Handling

| Concern | Approach |
|---|---|
| **Database credentials** | Never hardcoded — read from `DB_USER` / `DB_PASS` environment variables at startup; `appsettings.json` contains only a localhost placeholder. |
| **RabbitMQ credentials** | Read from `appsettings.json` / environment variables; `guest/guest` is the local-only default, overridable via env vars before production. |
| **CNPJ (taxpayer ID)** | Stored as plain text internally (required for filtering/search), but the API does not expose CNPJ in error messages or logs. |
| **Raw XML** | Full fiscal XML is stored in the `FiscalDocuments` table as it is legally required for audit purposes. Access is restricted to authenticated requests (auth layer to be added). |
| **Idempotency keys** | Stored as a SHA-256 hash (`DocumentKeys.KeyHash`), not the raw value. |
| **Connection strings** | Never logged; `appsettings.json` is excluded from version control via `.gitignore` in production setups. |

> In a production environment: use a secrets manager (Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault), enable TLS for PostgreSQL and RabbitMQ connections, and add JWT/API-key authentication to the API layer.

---

## Possible Improvements

Given more time, the following would be prioritised:

| Area | Improvement |
|---|---|
| **Authentication & authorisation** | Add JWT bearer or API-key middleware; restrict endpoints by role. |
| **Raw XML storage** | Move raw XML out of the main table into object storage (S3/Azure Blob) — keep only a reference URL to reduce DB row size at scale. |
| **SEFAZ XSD validation** | Load official SEFAZ XSD files into `XmlSchemaValidator` for full schema validation instead of structural-only checks. |
| **Outbox pattern** | Replace direct RabbitMQ publish with a transactional outbox (write event to DB in same transaction, relay asynchronously) to eliminate the publish/persist inconsistency window. |
| **Consumer scaling** | Extract `RabbitMqConsumer` to a separate worker service so it can be scaled independently of the API. |
| **Load testing** | Add NBomber or k6 scripts to measure ingest throughput and query latency under concurrent load. |
| **Architecture tests** | Add NetArchTest rules to enforce layer dependency constraints (e.g. Domain must not reference Infrastructure). |
| **Observability** | Add structured logging (Serilog), distributed tracing (OpenTelemetry), and metrics (Prometheus/Grafana). |
| **Soft delete** | Replace hard `DELETE` with a `DeletedAt` timestamp to preserve audit history. |

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments
- Write tests for new features
- Ensure all tests pass before submitting PR

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📚 Additional Resources

- [Brazilian Fiscal Documents Specification](https://www.nfe.fazenda.gov.br/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)

## 🆘 Support

For questions or issues, please open an issue on GitHub or contact the development team.
