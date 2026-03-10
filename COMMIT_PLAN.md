# Commit Plan

Ordered from first to last. Each commit is self-contained and builds on the previous one.

---

## 1. chore(repo): initialize solution and project structure

**Files:**
- `S13G.sln`
- `src/Domain/Domain.csproj`
- `src/Application/Application.csproj`
- `src/Infrastructure/Infrastructure.csproj`
- `src/Api/Api.csproj`
- `tests/Unit/UnitTests.csproj`
- `tests/Integration/IntegrationTests.csproj`
- `.gitignore`

**Body:**
Bootstraps the multi-project solution with clean architecture layers and test projects. No logic — only project references and package dependencies.

---

## 2. feat(domain): add core domain entities

**Files:**
- `src/Domain/Entities/FiscalDocument.cs`
- `src/Domain/Entities/DocumentKey.cs`
- `src/Domain/Entities/DocumentSummary.cs`
- `src/Domain/Entities/ProcessingEvent.cs`

**Body:**
Defines the aggregate root (FiscalDocument), its idempotency key companion (DocumentKey), the denormalized read-model used for fast listing (DocumentSummary), and a reserved audit-log entity (ProcessingEvent).

---

## 3. feat(infrastructure): add EF Core DbContext and initial migration

**Files:**
- `src/Infrastructure/Persistence/AppDbContext.cs`
- `src/Infrastructure/Persistence/AppDbContextFactory.cs`
- `src/Infrastructure/Persistence/Migrations/20260306143505_InitialCreate.cs`
- `src/Infrastructure/Persistence/Migrations/20260306143505_InitialCreate.Designer.cs`
- `src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Api/appsettings.json`

**Body:**
Wires EF Core with Npgsql, configures entity mappings, and creates the initial migration that produces the FiscalDocuments, DocumentKeys, DocumentSummaries, and ProcessingEvents tables.

---

## 4. feat(application): add repository and event-publisher interfaces

**Files:**
- `src/Application/Common/Interfaces/IFiscalDocumentRepository.cs`
- `src/Application/Common/Interfaces/IDocumentSummaryRepository.cs`
- `src/Application/Common/Interfaces/IEventPublisher.cs`
- `src/Application/Common/Interfaces/IXmlDocumentParser.cs`
- `src/Application/Common/Interfaces/IXmlSchemaValidator.cs`
- `src/Application/Common/Models/DocumentFilter.cs`
- `src/Application/Common/Models/PaginatedResult.cs`
- `src/Application/Common/Exceptions/XmlValidationException.cs`

**Body:**
Declares all ports the Application layer depends on. Concrete implementations live in Infrastructure; this commit keeps the dependency direction correct and makes the interfaces available before any handler is written.

---

## 5. feat(infrastructure): implement fiscal document repository

**Files:**
- `src/Infrastructure/Persistence/FiscalDocumentRepository.cs`
- `src/Infrastructure/Persistence/DocumentSummaryRepository.cs`

**Body:**
Implements idempotent insert (check-then-insert inside a transaction), paginated filtering, update, and delete for fiscal documents. DocumentSummaryRepository provides the append-only write path consumed by the RabbitMQ consumer.

---

## 6. feat(infrastructure): add composite query indexes migration

**Files:**
- `src/Infrastructure/Persistence/Migrations/20260306160000_AddQueryIndexes.cs`
- `src/Infrastructure/Persistence/Migrations/20260306160000_AddQueryIndexes.Designer.cs`
- `src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Body:**
Adds non-clustered indexes on IssuerCnpj, RecipientCnpj, State, and IssueDate to keep list queries fast at scale. Snapshot updated to reflect the new model state.

---

## 7. feat(infrastructure): implement XML document parser

**Files:**
- `src/Infrastructure/Xml/XmlDocumentParser.cs`

**Body:**
Streaming XmlReader-based parser that extracts DocumentKey, IssuerCnpj, RecipientCnpj, State, IssueDate, and TotalValue from NFe, CTe, and NFSe formats. Handles invalid non-XML preambles common in Brazilian fiscal documents.

---

## 8. feat(infrastructure): implement XML schema validator

**Files:**
- `src/Infrastructure/Xml/XmlSchemaValidator.cs`

**Body:**
Validates well-formedness, recognized root element (NFe/CTe/NFSe), and presence of the required info element (infNFe/infCte/infNFSe) before the document reaches the parser. Returns a structured error list rather than throwing immediately, so callers can surface all problems at once.

---

## 9. test(xml): add unit tests for parser and schema validator

**Files:**
- `tests/Unit/Infrastructure/Xml/XmlDocumentParserTests.cs`
- `tests/Unit/Infrastructure/Xml/XmlSchemaValidatorTests.cs`

**Body:**
Covers NFe/CTe/NFSe field extraction, preamble stripping, CNPJ section isolation (transporta/entrega must not override issuer), TryParse fallback on invalid decimals, well-formedness errors, unknown root, and missing required elements.

---

## 10. feat(application): add document ingestion command with idempotency

**Files:**
- `src/Application/Documents/IngestDocument/IngestDocumentCommand.cs`
- `src/Application/Documents/IngestDocument/IngestDocumentHandler.cs`
- `src/Application/Documents/IngestDocument/IngestDocumentValidator.cs`
- `src/Application/Events/DocumentProcessedEvent.cs`

**Body:**
Core write path: validates the stream, validates XML structure, parses fields, generates a SHA-256 idempotency key if none is provided, deduplicates via the repository, and publishes DocumentProcessedEvent. Any duplicate submission returns the original document without re-inserting.

---

## 11. test(application): add unit tests for ingest handler and validator

**Files:**
- `tests/Unit/Application/Documents/IngestDocumentHandlerTests.cs`
- `tests/Unit/Application/Documents/IngestDocumentValidatorTests.cs`

**Body:**
Verifies validation failure paths, idempotency behavior, correct event payload, SHA-256 key generation consistency, status set to Received, and that duplicate keys still trigger event publishing.

---

## 12. feat(application): add document query operations

**Files:**
- `src/Application/Documents/Queries/GetDocumentByIdQuery.cs`
- `src/Application/Documents/Queries/GetDocumentByIdHandler.cs`
- `src/Application/Documents/Queries/DocumentListQuery.cs`
- `src/Application/Documents/Queries/DocumentListHandler.cs`
- `src/Application/Documents/Queries/DocumentSummaryDto.cs`

**Body:**
Read side: single document retrieval and paginated list with optional filters on CNPJ, State, and date range.

---

## 13. test(application): add unit tests for query handlers

**Files:**
- `tests/Unit/Application/Documents/GetDocumentByIdHandlerTests.cs`
- `tests/Unit/Application/Documents/DocumentListHandlerTests.cs`

---

## 14. feat(application): add update and delete commands

**Files:**
- `src/Application/Documents/Commands/UpdateDocumentCommand.cs`
- `src/Application/Documents/Commands/UpdateDocumentHandler.cs`
- `src/Application/Documents/Commands/DeleteDocumentCommand.cs`
- `src/Application/Documents/Commands/DeleteDocumentHandler.cs`

---

## 15. test(application): add unit tests for update and delete handlers

**Files:**
- `tests/Unit/Application/Documents/UpdateDocumentHandlerTests.cs`
- `tests/Unit/Application/Documents/DeleteDocumentHandlerTests.cs`

---

## 16. feat(infrastructure): add RabbitMQ configuration and publisher

**Files:**
- `src/Infrastructure/Configuration/RabbitMqOptions.cs`
- `src/Infrastructure/Messaging/RabbitMqPublisher.cs`

**Body:**
Typed options model for RabbitMQ settings. Publisher uses publisher confirms (ConfirmSelect + WaitForConfirmsOrDie) and a Polly exponential-backoff retry policy to guarantee at-least-once delivery.

---

## 17. feat(infrastructure): add RabbitMQ consumer background service

**Files:**
- `src/Infrastructure/Messaging/RabbitMqConsumer.cs`

**Body:**
BackgroundService that consumes DocumentProcessedEvent messages, resolves a scoped IDocumentSummaryRepository per message, writes the denormalized summary, and routes failures to the dead-letter exchange rather than crashing the process.

---

## 18. test(infrastructure): add unit tests for messaging

**Files:**
- `tests/Unit/Infrastructure/Messaging/RabbitMqPublisherTests.cs`
- `tests/Unit/Infrastructure/Messaging/RabbitMqConsumerTests.cs`

**Body:**
Publisher tests verify BasicPublish + WaitForConfirmsOrDie are called, persistent delivery mode is set, transient failures are retried, and exhausted retries propagate. Consumer tests verify correct DocumentSummary field mapping, null-state default, fresh scope per invocation, and exception propagation.

---

## 19. feat(infrastructure): add PostgreSQL and RabbitMQ health checks

**Files:**
- `src/Infrastructure/HealthChecks/PostgreSqlHealthCheck.cs`
- `src/Infrastructure/HealthChecks/RabbitMqHealthCheck.cs`

**Body:**
Lightweight connectivity probes (SELECT 1 / CreateConnection + IsOpen) registered as named health checks exposed via GET /health. Allows load balancers and orchestrators to detect infrastructure failures without application-level impact.

---

## 20. test(persistence): add idempotency unit tests

**Files:**
- `tests/Unit/Infrastructure/Persistence/IdempotencyTests.cs`

**Body:**
Verifies that duplicate key submissions return the original document without creating a second row, and that concurrent read-path lookups (Task.WhenAll) all resolve to the same document ID.

---

## 21. feat(api): add REST controllers and request/response models

**Files:**
- `src/Api/Controllers/DocumentsController.cs`
- `src/Api/Models/DocumentDto.cs`
- `src/Api/Models/PagedResultDto.cs`
- `src/Api/Models/UpdateDocumentRequest.cs`
- `src/Api/Models/UploadDocumentRequest.cs`
- `src/Api/Validators/UpdateDocumentRequestValidator.cs`

**Body:**
Exposes POST /documents/upload (multipart), GET /documents, GET /documents/{id}, PATCH /documents/{id}, and DELETE /documents/{id}. Upload endpoint reads the Idempotency-Key header and returns 400 with structured errors on XML validation failure.

---

## 22. feat(api): configure DI, middleware, migrations, and health endpoint

**Files:**
- `src/Api/Program.cs`

**Body:**
Wires MediatR, FluentValidation, EF Core, RabbitMQ publisher/consumer, XML parser, XML schema validator, health checks, Swagger, and the /health JSON endpoint. Runs pending migrations automatically on startup.

---

## 23. test(integration): add integration test fixtures and factory

**Files:**
- `tests/Integration/IntegrationTestFactory.cs`
- `tests/Integration/Fixtures/DatabaseFixture.cs`
- `tests/Integration/Fixtures/RabbitMqFixture.cs`

**Body:**
Testcontainers v3 fixtures spin up isolated PostgreSQL and RabbitMQ containers per test session. WebApplicationFactory overrides connection strings so the API under test hits the containerized services.

---

## 24. test(integration): add end-to-end document upload tests

**Files:**
- `tests/Integration/DocumentUploadTests.cs`

**Body:**
End-to-end coverage: valid NFe/CTe/NFSe returns 201 and correct Type in DB, duplicate idempotency key returns same ID with one DB row, invalid XML returns 400 with error list, unrecognized root returns 400, missing file returns 400, published event lands in RabbitMQ queue.

---

## 25. chore(scripts): add setup and database init scripts

**Files:**
- `scripts/setup.sh`
- `scripts/setup-local-macos.sh`
- `scripts/setup-local-linux.sh`
- `scripts/setup-local-windows.ps1`
- `scripts/init-db.sql`
- `docker-compose.yml`
- `get-docker.sh`

**Body:**
Provides one-command local setup paths for Docker, macOS Homebrew, Linux apt, and Windows. The macOS script handles the RabbitMQ management plugin and Erlang PATH quirks. docker-compose.yml defines the canonical dev environment used by the Docker path.

---

## 26. chore(samples): add sample XML documents

**Files:**
- `sample_XML/NFe.xml`
- `sample_XML/cte.xml`
- `sample_XML/valid_NFe.xml`
- `sample_XML/malformed_XML.xml`
- `sample_XML/missing_infNFe.xml`
- `sample_XML/unrecognized_root.xml`

**Body:**
Reference documents for manual testing via Postman or curl. Includes valid and intentionally broken files that correspond to validation test cases.

---

## 27. chore(api): add Postman collection

**Files:**
- `S13G.postman_collection.json`

---

## 28. docs: add README

**Files:**
- `README.md`

---

## 29. docs: add architecture document

**Files:**
- `ARCHITECTURE.md`

---

## 30. docs: add local setup and running guide

**Files:**
- `PROJECT_RUNNING.md`
