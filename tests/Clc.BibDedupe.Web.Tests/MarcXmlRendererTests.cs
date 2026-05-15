using System;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Clc.BibDedupe.Web;

namespace Clc.BibDedupe.Web.Tests
{
    [TestClass]
    public class MarcXmlRendererTests
    {
        private const string XsltString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
  <xsl:template match=""/"">
    <Result><xsl:value-of select=""Source/Message"" /></Result>
  </xsl:template>
</xsl:stylesheet>";

        private const string MarcXml = "<Source><Message>Hello World</Message></Source>";
        private const string ExpectedOutput = "<Result>Hello World</Result>";

        [TestMethod]
        public void Transform_ShouldReturnTransformedXml_GivenValidXmlAndXslt()
        {
            var result = MarcXmlRenderer.Transform(MarcXml, XsltString);

            result.Should().Be(ExpectedOutput);
        }

        [TestMethod]
        public void TransformFile_ShouldReturnTransformedXml_GivenValidXmlAndXsltPath()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, XsltString);

                var result = MarcXmlRenderer.TransformFile(MarcXml, tempFile);

                result.Should().Be(ExpectedOutput);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void Transform_ShouldThrowXmlException_GivenInvalidXml()
        {
            var invalidXml = "<Source><Message>Hello World</Message></Invalid>";

            Action act = () => MarcXmlRenderer.Transform(invalidXml, XsltString);

            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Transform_ShouldThrowXsltException_GivenInvalidXslt()
        {
            var invalidXslt = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
  <xsl:template match=""/"">
    <Result><xsl:value-of select=""Source/Message"" </Result>
  </xsl:template>
</xsl:stylesheet>";

            Action act = () => MarcXmlRenderer.Transform(MarcXml, invalidXslt);

            act.Should().Throw<Exception>(); // Catch base Exception as it could be XmlException or XsltException during load
        }
    }
}
