using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using S13G.Tests.Integration.Fixtures;

namespace S13G.Tests.Integration
{
    [TestFixture]
    public class DocumentUploadTests
    {
        private DatabaseFixture _dbFixture;
        private RabbitMqFixture _rabbitFixture;
        private IntegrationTestFactory _factory;
        private HttpClient _client;

        private const string ValidNFe =
            "<NFe><infNFe><ide><chNFe>UNIQUE-KEY-001</chNFe><dhEmi>2023-01-01T00:00:00</dhEmi><cUF>35</cUF></ide>" +
            "<emit><CNPJ>00000000000191</CNPJ></emit><det><prod><vProd>10</vProd></prod></det></infNFe></NFe>";

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _dbFixture = new DatabaseFixture();
            await _dbFixture.InitializeAsync();

            _rabbitFixture = new RabbitMqFixture();
            await _rabbitFixture.InitializeAsync();

            _factory = new IntegrationTestFactory(
                _dbFixture.Container.GetConnectionString(),
                _rabbitFixture.Container.Hostname);
            _client = _factory.CreateClient();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            _client.Dispose();
            _factory.Dispose();
            await _dbFixture.DisposeAsync();
            await _rabbitFixture.DisposeAsync();
        }

        // ── Helper ─────────────────────────────────────────────────────────────

        private static MultipartFormDataContent BuildXmlContent(string xml)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/xml") }
            }, "file", "doc.xml");
            return content;
        }

        // ── Tests ──────────────────────────────────────────────────────────────

        [Test]
        [Order(1)]
        public async Task PostUpload_ValidXml_ReturnsCreatedAndPersists()
        {
            using var content = BuildXmlContent(ValidNFe);
            content.Headers.Add("Idempotency-Key", "test-key-001");

            var response = await _client.PostAsync("/documents/upload", content);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().Contain("id");

            using var db = new Infrastructure.Persistence.AppDbContext(
                new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                    .UseNpgsql(_dbFixture.Container.GetConnectionString())
                    .Options);
            var count = await db.FiscalDocuments.CountAsync();
            count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Test]
        [Order(2)]
        public async Task PostUpload_SameIdempotencyKeyTwice_ReturnsSameIdAndOnlyOneDbRow()
        {
            const string idempotencyKey = "dedup-key-fixed";

            using var content1 = BuildXmlContent(ValidNFe);
            content1.Headers.Add("Idempotency-Key", idempotencyKey);
            var response1 = await _client.PostAsync("/documents/upload", content1);
            response1.StatusCode.Should().Be(HttpStatusCode.Created);
            var id1 = JsonDocument.Parse(await response1.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetString();

            using var content2 = BuildXmlContent(ValidNFe);
            content2.Headers.Add("Idempotency-Key", idempotencyKey);
            var response2 = await _client.PostAsync("/documents/upload", content2);
            response2.StatusCode.Should().Be(HttpStatusCode.Created);
            var id2 = JsonDocument.Parse(await response2.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetString();

            id1.Should().Be(id2);

            using var db = new Infrastructure.Persistence.AppDbContext(
                new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                    .UseNpgsql(_dbFixture.Container.GetConnectionString())
                    .Options);
            var keyCount = await db.DocumentKeys.CountAsync(k => k.KeyHash == idempotencyKey);
            keyCount.Should().Be(1);
        }

        [Test]
        public async Task PostUpload_InvalidXml_Returns400WithErrorList()
        {
            using var content = BuildXmlContent("<totally-broken-xml");

            var response = await _client.PostAsync("/documents/upload", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
        }

        [Test]
        public async Task PostUpload_UnrecognizedRootElement_Returns400()
        {
            using var content = BuildXmlContent("<Invoice><Total>100</Total></Invoice>");

            var response = await _client.PostAsync("/documents/upload", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Unrecognized root element");
        }

        [Test]
        public async Task PostUpload_NoFile_Returns400()
        {
            var response = await _client.PostAsync("/documents/upload", new MultipartFormDataContent());
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        [Order(3)]
        public async Task PostUpload_ValidXml_EventLandsInRabbitMqQueue()
        {
            using var content = BuildXmlContent(ValidNFe);
            content.Headers.Add("Idempotency-Key", "rabbit-test-key");

            await _client.PostAsync("/documents/upload", content);

            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = _rabbitFixture.Container.Hostname,
                Port = _rabbitFixture.Container.GetMappedPublicPort(5672)
            };
            using var conn = factory.CreateConnection();
            using var channel = conn.CreateModel();

            // retry a few times to allow async consumer to route the message
            RabbitMQ.Client.BasicGetResult result = null;
            for (var i = 0; i < 5 && result == null; i++)
            {
                result = channel.BasicGet("documents.processed", autoAck: true);
                if (result == null) await Task.Delay(500);
            }

            result.Should().NotBeNull("event should be published to the queue");
            var eventBody = Encoding.UTF8.GetString(result.Body.ToArray());
            eventBody.Should().Contain("documentId");
            eventBody.Should().Contain("cnpj");
        }

        private const string ValidCTe =
            "<CTe><infCTe Id=\"CTe35240100000000000001\">" +
            "<ide><dhEmi>2024-01-01T10:00:00</dhEmi><cUF>35</cUF></ide>" +
            "<emit><CNPJ>55555555000100</CNPJ></emit>" +
            "<vCTe>800.00</vCTe>" +
            "</infCTe></CTe>";

        private const string ValidNFSe =
            "<CompNfse><NFSe><infNFSe Id=\"NFSe001\">" +
            "<dhEmissao>2024-02-01T09:00:00</dhEmissao>" +
            "<prest><CNPJ>66666666000100</CNPJ><UF>RJ</UF></prest>" +
            "<vLiq>300.00</vLiq>" +
            "</infNFSe></NFSe></CompNfse>";

        [Test]
        [Order(4)]
        public async Task PostUpload_ValidCTe_ReturnsCreatedAndPersistsWithCorrectType()
        {
            using var content = BuildXmlContent(ValidCTe);
            content.Headers.Add("Idempotency-Key", "cte-type-test-key");

            var response = await _client.PostAsync("/documents/upload", content);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var id = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetString();
            id.Should().NotBeNullOrEmpty();

            using var db = new Infrastructure.Persistence.AppDbContext(
                new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                    .UseNpgsql(_dbFixture.Container.GetConnectionString())
                    .Options);
            var doc = await db.FiscalDocuments.FindAsync(Guid.Parse(id));
            doc.Should().NotBeNull();
            doc.Type.Should().Be(Domain.Entities.DocumentType.CTe);
        }

        [Test]
        [Order(5)]
        public async Task PostUpload_ValidNFSe_ReturnsCreatedAndPersistsWithCorrectType()
        {
            using var content = BuildXmlContent(ValidNFSe);
            content.Headers.Add("Idempotency-Key", "nfse-type-test-key");

            var response = await _client.PostAsync("/documents/upload", content);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var id = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetString();
            id.Should().NotBeNullOrEmpty();

            using var db = new Infrastructure.Persistence.AppDbContext(
                new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                    .UseNpgsql(_dbFixture.Container.GetConnectionString())
                    .Options);
            var doc = await db.FiscalDocuments.FindAsync(Guid.Parse(id));
            doc.Should().NotBeNull();
            doc.Type.Should().Be(Domain.Entities.DocumentType.NFSe);
        }

    }
}

