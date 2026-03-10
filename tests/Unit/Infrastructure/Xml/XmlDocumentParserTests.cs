using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using S13G.Domain.Entities;
using S13G.Infrastructure.Xml;

namespace S13G.Tests.Unit.Infrastructure.Xml
{
    [TestFixture]
    public class XmlDocumentParserTests
    {
        private XmlDocumentParser _parser;

        [SetUp]
        public void Setup() => _parser = new XmlDocumentParser();

        [Test]
        public async Task ParseAsync_MinimalNFe_ExtractsFieldsCorrectly()
        {
            const string xml =
"<NFe><infNFe><ide><chNFe>123</chNFe><dhEmi>2023-01-01T10:00:00</dhEmi><cUF>35</cUF></ide><emit><CNPJ>00000000000191</CNPJ></emit><det><prod><vProd>100.00</vProd></prod></det></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.DocumentKey.Should().Be("123");
            result.IssueDate.Should().BeCloseTo(System.DateTime.Parse("2023-01-01T10:00:00"), System.TimeSpan.FromSeconds(1));
            result.State.Should().Be("35");
            result.IssuerCnpj.Should().Be("00000000000191");
            result.Type.Should().Be(DocumentType.NFe);
            result.RawXml.Should().Contain("<NFe>");
        }

        [Test]
        public async Task ParseAsync_NfeWithNonXmlPreamble_StripsAndParsesCorrectly()
        {
            const string xml =
"<-- Powered By WebDANFE --><NFe><infNFe><ide><chNFe>ABC</chNFe><dhEmi>2024-06-01T08:00:00</dhEmi><cUF>SP</cUF></ide><emit><CNPJ>11111111000100</CNPJ></emit><total><ICMSTot><vNF>500.00</vNF></ICMSTot></total></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.DocumentKey.Should().Be("ABC");
            result.IssuerCnpj.Should().Be("11111111000100");
            result.Type.Should().Be(DocumentType.NFe);
        }

        [Test]
        public async Task ParseAsync_MinimalCTe_ExtractsTypeAndIssuerCnpj()
        {
            const string xml =
"<cteProc><CTe><infCte Id=\"CTe35240100000000000000\"><ide><dhEmi>2024-03-01T10:00:00</dhEmi><cUF>35</cUF></ide><emit><CNPJ>22222222000100</CNPJ></emit></infCte></CTe></cteProc>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.Type.Should().Be(DocumentType.CTe);
            result.IssuerCnpj.Should().Be("22222222000100");
        }

        [Test]
        public async Task ParseAsync_MinimalNFSe_ExtractsTypeAndIssuerCnpj()
        {
            const string xml =
"<CompNfse><NFSe><infNFSe Id=\"NFSe001\"><dhEmissao>2024-04-15T09:00:00</dhEmissao><prest><CNPJ>33333333000100</CNPJ><UF>RJ</UF></prest><vLiq>150.00</vLiq></infNFSe></NFSe></CompNfse>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.Type.Should().Be(DocumentType.NFSe);
            result.IssuerCnpj.Should().Be("33333333000100");
        }

        [Test]
        public async Task ParseAsync_NfeWithDestSection_ExtractsRecipientCnpj()
        {
            const string xml =
"<NFe><infNFe><emit><CNPJ>11111111000100</CNPJ></emit><dest><CNPJ>99999999000100</CNPJ></dest></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.IssuerCnpj.Should().Be("11111111000100");
            result.RecipientCnpj.Should().Be("99999999000100");
        }

        [Test]
        public async Task ParseAsync_MissingOptionalFields_CompletesWithoutError()
        {
            const string xml = "<NFe><infNFe/></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var act = async () => await _parser.ParseAsync(stream);
            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task ParseAsync_CnpjInTransportaSection_DoesNotOverrideIssuerCnpj()
        {
            // transporta is a carrier section — its CNPJ must not land in IssuerCnpj or RecipientCnpj
            const string xml =
"<NFe><infNFe><emit><CNPJ>11111111000100</CNPJ></emit>" +
"<transp><transporta><CNPJ>77777777000100</CNPJ></transporta></transp></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.IssuerCnpj.Should().Be("11111111000100");
            result.RecipientCnpj.Should().NotBe("77777777000100");
        }

        [Test]
        public async Task ParseAsync_CnpjInEntregaSection_DoesNotOverrideIssuerCnpj()
        {
            // entrega is a delivery address section — its CNPJ must not land in IssuerCnpj
            const string xml =
"<NFe><infNFe><emit><CNPJ>22222222000100</CNPJ></emit>" +
"<entrega><CNPJ>88888888000100</CNPJ></entrega></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.IssuerCnpj.Should().Be("22222222000100");
            result.RecipientCnpj.Should().NotBe("88888888000100");
        }

        [Test]
        public async Task ParseAsync_InvalidDecimalInVnf_TotalValueRemainsZero()
        {
            const string xml =
"<NFe><infNFe><total><ICMSTot><vNF>NOT_A_NUMBER</vNF></ICMSTot></total></infNFe></NFe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await _parser.ParseAsync(stream);
            result.TotalValue.Should().Be(0m);
        }

        [Test]
        public async Task ParseAsync_CteWithVcteField_ExtractsTotalValue()
        {
            const string xml =
"<cteProc><CTe><infCte Id=\"CTe35240100000000000000\">" +
"<ide><dhEmi>2024-01-01T10:00:00</dhEmi><cUF>35</cUF></ide>" +
"<emit><CNPJ>44444444000100</CNPJ></emit>" +
"<vPrest><vTPrest>1500.75</vTPrest></vPrest>" +
"<infCTeNorm><infCarga><vCarga>1500.75</vCarga></infCarga></infCTeNorm>" +
"</infCte></CTe></cteProc>";
            // Use a CTe with direct vCTe node
            const string xmlWithVcte =
"<CTe><infCTe><emit><CNPJ>44444444000100</CNPJ></emit><vCTe>2500.50</vCTe></infCTe></CTe>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlWithVcte));
            var result = await _parser.ParseAsync(stream);
            result.TotalValue.Should().Be(2500.50m);
            result.Type.Should().Be(DocumentType.CTe);
        }
    }
}
