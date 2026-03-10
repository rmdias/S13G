using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using S13G.Application.Common.Interfaces;
using S13G.Domain.Entities;

namespace S13G.Infrastructure.Xml
{
    public class XmlDocumentParser : IXmlDocumentParser
    {
        public async Task<FiscalDocument> ParseAsync(Stream xmlStream, CancellationToken ct = default)
        {
            // Read entire content upfront — simplifies handling and lets us capture RawXml without rewinding
            string rawContent;
            using (var sr = new StreamReader(xmlStream, leaveOpen: false))
                rawContent = await sr.ReadToEndAsync(ct);

            // Strip any non-XML preamble before the root element (e.g. "<-- Powered By WebDANFE -->")
            var xmlStart = 0;
            for (var i = 0; i < rawContent.Length - 1; i++)
            {
                if (rawContent[i] == '<' && (rawContent[i + 1] == '?' || char.IsLetter(rawContent[i + 1])))
                {
                    xmlStart = i;
                    break;
                }
            }
            var xmlContent = xmlStart > 0 ? rawContent[xmlStart..] : rawContent;

            var doc = new FiscalDocument { RecipientCnpj = string.Empty, RawXml = xmlContent };

            using var reader = XmlReader.Create(new StringReader(xmlContent), new XmlReaderSettings
            {
                Async = true,
                IgnoreComments = true,
                IgnoreWhitespace = true
            });

            // Track current section to assign CNPJ and UF to the right party
            string currentSection = null;
            string root = null;

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (root == null)
                    {
                        root = reader.Name;
                        doc.Type = DetermineType(root);
                    }

                    // NF-e / CT-e / NFS-e store the access key as an Id attribute on infNFe/infCTe/infNFSe
                    if ((reader.Name == "infNFe" || reader.Name == "infCTe" || reader.Name == "infNFSe") && reader.HasAttributes)
                    {
                        var idAttr = reader.GetAttribute("Id");
                        if (!string.IsNullOrEmpty(idAttr))
                        {
                            // strip the 3-char type prefix ("NFe" / "CTe" / "NFS") if present
                            doc.DocumentKey = idAttr.Length > 3 && (idAttr.StartsWith("NFe") || idAttr.StartsWith("CTe") || idAttr.StartsWith("NFS"))
                                ? idAttr[3..]
                                : idAttr;
                        }
                    }

                    switch (reader.Name)
                    {
                        // Section markers — determines which party a CNPJ/UF belongs to
                        case "emit":   // NFe issuer
                        case "prest":  // NFSe issuer
                        case "dest":   // NFe recipient
                        case "toma":   // NFSe recipient
                        case "retirada":
                        case "entrega":
                        case "transporta":
                            currentSection = reader.Name;
                            break;

                        case "chNFe":
                        case "chCTe":
                        case "id": // NFSe
                            if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                                doc.DocumentKey = reader.Value.Trim();
                            break;

                        case "CNPJ":
                            if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                            {
                                var cnpj = reader.Value.Trim();
                                if (currentSection is "emit" or "prest")
                                    doc.IssuerCnpj = cnpj;
                                else if (currentSection is "dest" or "toma")
                                    doc.RecipientCnpj = cnpj;
                            }
                            break;

                        case "UF":
                        case "cUF":
                            // Only capture UF from the issuer section; ignore transport/delivery UFs
                            if (currentSection is null or "emit" or "prest" or "infNFe" or "infCTe" or "infNFSe")
                                if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                                    doc.State = reader.Value.Trim();
                            break;

                        case "dhEmi":
                        case "dEmi":
                        case "dhEmissao":
                            if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text &&
                                DateTime.TryParse(reader.Value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                                doc.IssueDate = dt;
                            break;

                        case "vNF":
                        case "vCTe":
                        case "vLiq":  // NFSe net value
                        case "valor":
                            if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text &&
                                decimal.TryParse(reader.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                                doc.TotalValue = v;
                            break;
                    }
                }
            }

            return doc;
        }

        private DocumentType DetermineType(string root)
        {
            if (root.Contains("NFe", StringComparison.OrdinalIgnoreCase)) return DocumentType.NFe;
            if (root.Contains("CTe", StringComparison.OrdinalIgnoreCase)) return DocumentType.CTe;
            if (root.IndexOf("NFSe", StringComparison.OrdinalIgnoreCase) >= 0) return DocumentType.NFSe;
            // fallback
            return DocumentType.NFe;
        }
    }
}