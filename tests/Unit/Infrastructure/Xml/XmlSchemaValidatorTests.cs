using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using S13G.Infrastructure.Xml;

namespace S13G.Tests.Unit.Infrastructure.Xml
{
    [TestFixture]
    public class XmlSchemaValidatorTests
    {
        private XmlSchemaValidator _validator;

        [SetUp]
        public void Setup() => _validator = new XmlSchemaValidator();

        [Test]
        public async Task ValidateAsync_ValidNFe_ReturnsValid()
        {
            const string xml = "<nfeProc><NFe><infNFe Id=\"NFe123\"/></NFe></nfeProc>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task ValidateAsync_ValidCTe_ReturnsValid()
        {
            const string xml = "<cteProc><CTe><infCTe Id=\"CTe456\"/></CTe></cteProc>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task ValidateAsync_ValidCTeLowercaseT_ReturnsValid()
        {
            const string xml = "<cteProc><CTe><infCte Id=\"CTe456\"/></CTe></cteProc>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task ValidateAsync_ValidNFSe_ReturnsValid()
        {
            const string xml = "<CompNfse><NFSe><infNFSe Id=\"NFSe789\"/></NFSe></CompNfse>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task ValidateAsync_MalformedXml_ReturnsErrorWithWellFormedMessage()
        {
            const string xml = "<nfeProc><NFe><infNFe";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainMatch("*not well-formed*");
        }

        [Test]
        public async Task ValidateAsync_UnrecognizedRoot_ReturnsErrorWithRootName()
        {
            const string xml = "<Invoice><infNFe/></Invoice>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainMatch("*Invoice*");
        }

        [Test]
        public async Task ValidateAsync_MissingInfoElement_ReturnsRequiredElementError()
        {
            const string xml = "<nfeProc><NFe><someOtherElement/></NFe></nfeProc>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainMatch("*infNFe*");
        }

        [Test]
        public async Task ValidateAsync_EmptyString_ReturnsNoRootElementError()
        {
            var result = await _validator.ValidateAsync(string.Empty);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Test]
        public async Task ValidateAsync_WithNonXmlPreamble_StripsAndValidatesSuccessfully()
        {
            const string xml = "<-- Powered By WebDANFE --><NFe><infNFe Id=\"NFe123\"/></NFe>";
            var result = await _validator.ValidateAsync(xml);
            result.IsValid.Should().BeTrue();
        }
    }
}
