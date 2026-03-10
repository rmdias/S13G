# S13G — Início Rápido

## Option A: Docker Compose (Recomendado)

Requer o Docker em execução na sua máquina.

```bash
# Inicia os contêineres do PostgreSQL e RabbitMQ, restaura pacotes, compila e aplica as migrations
bash scripts/setup.sh

# Em seguida, inicie a API
cd src/Api && dotnet run
```

---

## Option B: Instalação Local (macOS com Homebrew)

Instala o PostgreSQL e o RabbitMQ via Homebrew, cria o banco de dados e aplica as migrations.

```bash
bash scripts/setup-local-macos.sh

# Em seguida, inicie a API
cd src/Api && dotnet run
```

**Gerenciar serviços:**
```bash
brew services start postgresql@15
brew services start rabbitmq
brew services stop postgresql@15
brew services stop rabbitmq
```

---

## Option C: Instalação Local (Linux)

```bash
sudo bash scripts/setup-local-linux.sh

cd src/Api && dotnet run
```

**Gerenciar serviços:**
```bash
sudo systemctl start postgresql
sudo systemctl start rabbitmq-server
```

---

## Option D: Instalação Local (Windows)

Execute o PowerShell como Administrador:

```powershell
.\scripts\setup-local-windows.ps1
```

Em seguida, inicie a API:
```bash
cd src/Api && dotnet run
```

---

## Após a Configuração: Endpoints da API

- **HTTP**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **HTTPS**: https://localhost:5001 (requer `dotnet dev-certs https --trust`)

---

## Teste de Fumaça

```bash
# Retorna a spec OpenAPI — funciona sem banco de dados
curl http://localhost:5000/swagger/v1/swagger.json

# Verificação de saúde — reporta o status do postgresql e do rabbitmq
curl http://localhost:5000/health

# Abre o Swagger UI (macOS)
open http://localhost:5000/swagger

# Abre o Swagger UI (Windows)
start http://localhost:5000/swagger
```

---

## Serviços Externos — Configurações Padrão

### PostgreSQL

| Configuração | Valor |
|---|---|
| Host | localhost:5432 |
| Banco de dados | s13g |
| Usuário | postgres |
| Senha | postgres |

Substitua as credenciais em tempo de execução sem editar o `appsettings.json`:

```bash
export DB_USER=myuser
export DB_PASS=mypassword
cd src/Api && dotnet run
```

> Em instalações via Homebrew no macOS, o Postgres usa por padrão o usuário atual do sistema operacional sem senha.
> O `setup-local-macos.sh` trata isso automaticamente definindo `DB_USER=$(whoami)`.
>
> Para uma substituição local permanente, crie o arquivo `src/Api/appsettings.Development.json` (ignorado pelo git)
> com a sua connection string. Um modelo está disponível em
> `src/Api/appsettings.Development.json.example` — copie-o e preencha com seus valores.
> O `launchSettings.json` define `ASPNETCORE_ENVIRONMENT=Development` automaticamente ao
> executar via `dotnet run`, portanto o arquivo é carregado sem nenhuma configuração adicional de variável de ambiente.

### RabbitMQ

| Configuração | Valor |
|---|---|
| Host | localhost:5672 |
| Usuário | guest |
| Senha | guest |
| Exchange | `documents` |
| Fila | `documents.processed` |
| Dead-letter exchange | `documents.dlx` |
| Interface de gerenciamento | http://localhost:15672 (guest/guest) |

---

## O Que Funciona Sem Serviços Externos

| Funcionalidade | Requer |
|---|---|
| Swagger UI | Nada |
| Roteamento da API REST | Nada |
| Upload de documentos | PostgreSQL + RabbitMQ |
| Listagem / consulta de documentos | PostgreSQL |
| Consumidor RabbitMQ | RabbitMQ |

Erros de banco de dados e falhas do RabbitMQ são registrados em log e tratados de forma adequada — a API não irá travar.

---

## Informações do Projeto

- **Framework**: .NET 10.0
- **Porta da API**: 5000 (HTTP) / 5001 (HTTPS)
- **Banco de dados**: PostgreSQL (configurado em `src/Api/appsettings.json`)
- **Fila de mensagens**: RabbitMQ (configurado em `src/Api/appsettings.json`)
- **Documentação**: [README.md](README.md) — configuração completa | [ARCHITECTURE.md](ARCHITECTURE.md) — documentação de arquitetura
