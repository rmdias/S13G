using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using S13G.Application.Common.Interfaces;

namespace S13G.Infrastructure.Xml
{
    /// <summary>
    /// Validates fiscal XML documents (NFe, CTe, NFSe) for well-formedness and structure.
    /// Designed to accept embedded SEFAZ XSD files in a future iteration — currently performs
    /// structural validation: well-formedness, recognised root element, and required info element.
    /// </summary>
    public class XmlSchemaValidator : IXmlSchemaValidator
    {
        private static readonly HashSet<string> KnownRoots = new(StringComparer.OrdinalIgnoreCase)
        {
            "nfeProc", "NFe",
            "cteProc", "CTe",
            "CompNfse", "NFSe", "nfseProc"
        };

        public Task<XmlValidationResult> ValidateAsync(string xmlContent, CancellationToken ct = default)
        {
            var errors = new List<string>();

            try
            {
                // Strip any non-XML preamble (e.g. "<-- Powered By WebDANFE -->") — same logic as XmlDocumentParser
                var xmlStart = 0;
                for (var i = 0; i < xmlContent.Length - 1; i++)
                {
                    if (xmlContent[i] == '<' && (xmlContent[i + 1] == '?' || char.IsLetter(xmlContent[i + 1])))
                    {
                        xmlStart = i;
                        break;
                    }
                }
                if (xmlStart > 0)
                    xmlContent = xmlContent[xmlStart..];

                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.None,
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                };

                string rootElement = null;
                bool hasInfoElement = false;

                using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
                while (reader.Read())
                {
                    ct.ThrowIfCancellationRequested();

                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    if (rootElement == null)
                    {
                        rootElement = reader.Name;
                        if (!KnownRoots.Contains(rootElement))
                            errors.Add($"Unrecognized root element '{rootElement}'. Expected NFe, CTe, or NFSe document.");
                    }

                    if (reader.Name is "infNFe" or "infCTe" or "infCte" or "infNFSe")
                        hasInfoElement = true;
                }

                if (rootElement == null)
                    errors.Add("XML document has no root element.");
                else if (!hasInfoElement && errors.Count == 0)
                    errors.Add("Required element infNFe, infCTe, or infNFSe not found.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (XmlException ex)
            {
                errors.Add($"XML is not well-formed: {ex.Message}");
            }

            return Task.FromResult(new XmlValidationResult(errors.Count == 0, errors));
        }
    }
}
