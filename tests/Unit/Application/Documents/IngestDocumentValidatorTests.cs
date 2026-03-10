using System.IO;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using S13G.Application.Documents.IngestDocument;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class IngestDocumentValidatorTests
    {
        private IngestDocumentValidator _validator;

        [SetUp]
        public void Setup() => _validator = new IngestDocumentValidator();

        [Test]
        public void Validate_NullStream_HasValidationError()
        {
            var cmd = new IngestDocumentCommand(null);
            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.XmlStream)
                  .WithErrorMessage("XML payload is required.");
        }

        [Test]
        public void Validate_ValidReadableStream_PassesValidation()
        {
            var cmd = new IngestDocumentCommand(new MemoryStream(new byte[] { 0x3C }));
            var result = _validator.TestValidate(cmd);
            result.ShouldNotHaveValidationErrorFor(x => x.XmlStream);
        }

        [Test]
        public void Validate_NonReadableStream_HasValidationError()
        {
            // A closed stream is not readable
            var stream = new MemoryStream();
            stream.Close();

            var cmd = new IngestDocumentCommand(stream);
            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.XmlStream)
                  .WithErrorMessage("XML stream must be readable.");
        }
    }
}
