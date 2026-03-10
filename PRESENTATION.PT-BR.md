# S13G — Apresentação

---

## Slide 1 — Título

# S13G: Sistema de Processamento de Documentos Fiscais Brasileiros

**Backend orientado a eventos para documentos XML: NFe, CTe e NFSe**

- ASP.NET Core · .NET 10
- PostgreSQL · RabbitMQ · Docker
- Clean Architecture · CQRS · REST API

> Live: https://s13g.onrender.com/swagger

---

## Slide 2 — Problema

# O Problema

Empresas brasileiras são obrigadas por lei a receber, armazenar e processar documentos fiscais XML emitidos pelo governo.

| Desafio | Impacto |
|---|---|
| Documentos chegam em volume | Processamento deve ser rápido e confiável |
| Sem deduplicação nativa | Mesmo documento ingerido várias vezes |
| Regras rígidas de schema (SEFAZ) | Documentos inválidos devem ser rejeitados cedo |
| Documentos devem ser consultáveis | XML bruto sozinho não é suficiente |

**Processamento manual é lento, propenso a erros e não escala.**

---

## Slide 3 — Visão Geral da Solução

# Visão Geral da Solução

Uma REST API que gerencia o ciclo de vida completo dos documentos fiscais.

- **Upload** — aceita XML via multipart form
- **Validar** — verifica estrutura e tipo do documento
- **Parsear** — extrai metadados (CNPJ, estado, valor, data)
- **Deduplicar** — chave de idempotência evita ingestão dupla
- **Persistir** — armazena documento no PostgreSQL
- **Notificar** — publica evento no RabbitMQ para consumidores assíncronos

Um endpoint gerencia a ingestão. O restante do sistema reage a eventos.

---

## Slide 4 — Arquitetura

# Arquitetura

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

**Regra de dependência:** camadas externas dependem das internas — nunca o inverso.

---

## Slide 5 — Fluxo Orientado a Eventos

# Fluxo Orientado a Eventos

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

## Slide 6 — Principais Decisões Técnicas

# Principais Decisões Técnicas

| Decisão | Motivo |
|---|---|
| PostgreSQL ao invés de NoSQL | Consultas relacionais, transações ACID |
| Chave de idempotência SHA-256 | Dedup em O(1), sem varredura completa |
| Read-model DocumentSummary | Consultas de lista ignoram blobs XML |
| Polly com backoff exponencial | Falhas transientes com retry automático |
| Publisher confirms | Confirmação garantida pelo broker |
| Dead-letter exchange | Mensagens falhas arquivadas, não perdidas |
| Migrations EF Core no startup | Schema sempre sincronizado no deploy |

---

## Slide 7 — Endpoints da API

# Endpoints da API

| Método | Caminho | Descrição |
|---|---|---|
| `POST` | `/documents/upload` | Upload XML (multipart); header `Idempotency-Key` |
| `GET` | `/documents` | Lista paginada; filtros: CNPJ, estado, intervalo de datas |
| `GET` | `/documents/{id}` | Documento único + XML bruto |
| `PUT` | `/documents/{id}` | Atualiza estado / status |
| `DELETE` | `/documents/{id}` | Exclusão permanente |
| `GET` | `/health` | Status do PostgreSQL + RabbitMQ |

**Tipos de documento aceitos:** NFe · CTe · NFSe

XML inválido retorna `400` com lista de erros estruturada — não um 500 genérico.

---

## Slide 8 — Estratégia de Testes

# Estratégia de Testes

### Testes Unitários — NUnit · Moq · FluentAssertions

- Parser XML: extração de campos por tipo
- Handler de ingestão: fluxo principal + idempotência
- Handlers de query: filtros, paginação, não encontrado
- Idempotência: chave duplicada → mesmo ID

### Testes de Integração — Testcontainers

- PostgreSQL + RabbitMQ reais por execução
- POST XML → assert 201 + linha no DB + mensagem na fila
- Chave duplicada → mesmo ID, uma linha no DB
- XML inválido → 400 + lista de erros

### CI — GitHub Actions

- Executa a cada push e pull request
- Build → testes unitários → testes de integração (Docker necessário)

---

## Slide 9 — Deploy em Produção

# Deploy em Produção

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

| Serviço | Provedor | Plano |
|---|---|---|
| API | Render | Gratuito |
| PostgreSQL | Render | Gratuito |
| RabbitMQ | CloudAMQP | Little Lemur (gratuito) |

> **Live:** https://s13g.onrender.com/swagger
> Primeira requisição após 15 min inativo: ~30–60s de cold start

---

## Slide 10 — Próximos Passos

# Próximos Passos

| Área | Melhoria |
|---|---|
| **Segurança** | Autenticação JWT / API-key |
| **Armazenamento** | XML bruto para S3/Azure Blob; salvar URL de referência |
| **Validação** | Validação completa do schema XSD SEFAZ |
| **Confiabilidade** | Padrão Outbox — eliminar race de publicação/persistência |
| **Escalabilidade** | Extrair consumer para worker service independente |
| **Observabilidade** | OpenTelemetry · Serilog · Prometheus/Grafana |
| **Auditoria** | Soft delete — preservar histórico com `DeletedAt` |

Estes são trade-offs conhecidos e aceitos para um escopo de demonstração.
A arquitetura suporta todas essas melhorias sem alterações nas camadas.
