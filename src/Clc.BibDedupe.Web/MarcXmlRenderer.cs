using System.Text;
using System.Xml.Xsl;
using System.Xml;

namespace Clc.BibDedupe.Web
{
    public static class MarcXmlRenderer
    {
        // Transform with XSLT from a string
        public static string Transform(string marcXml, string xsltString)
        {
            var xslt = new XslCompiledTransform();
            var xsltSettings = new XsltSettings(enableDocumentFunction: false, enableScript: false);
            using var xsltReader = XmlReader.Create(new StringReader(xsltString),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            xslt.Load(xsltReader, xsltSettings, new XmlUrlResolver());

            using var xmlReader = XmlReader.Create(new StringReader(marcXml),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });

            var sb = new StringBuilder();
            using var writer = XmlWriter.Create(new StringWriter(sb),
                new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment });

            xslt.Transform(xmlReader, null, writer);
            return sb.ToString();
        }

        // Transform with XSLT from a file
        public static string TransformFile(string marcXml, string xsltPath)
            => Transform(marcXml, File.ReadAllText(xsltPath));
    }
}
