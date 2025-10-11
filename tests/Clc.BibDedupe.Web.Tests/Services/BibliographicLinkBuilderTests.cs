using Clc.BibDedupe.Web.Options;
using Clc.BibDedupe.Web.Services;
using FluentAssertions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class BibliographicLinkBuilderTests
{
    [TestMethod]
    public void BuildLink_Replaces_Default_Placeholder()
    {
        var options = OptionsFactory.Create(new BibliographicRecordLinkOptions
        {
            UrlTemplate = "https://example.test/bib/{bibId}"
        });
        var builder = new BibliographicLinkBuilder(options);

        var result = builder.BuildLink(12345);

        result.Should().Be("https://example.test/bib/12345");
    }

    [TestMethod]
    public void BuildLink_Returns_Null_When_Template_Is_Not_Set()
    {
        var options = OptionsFactory.Create(new BibliographicRecordLinkOptions());
        var builder = new BibliographicLinkBuilder(options);

        var result = builder.BuildLink(77);

        result.Should().BeNull();
    }

    [TestMethod]
    public void BuildLink_Uses_Format_Placeholder_When_Present()
    {
        var options = OptionsFactory.Create(new BibliographicRecordLinkOptions
        {
            UrlTemplate = "https://example.test/record/{0}"
        });
        var builder = new BibliographicLinkBuilder(options);

        var result = builder.BuildLink(42);

        result.Should().Be("https://example.test/record/42");
    }

    [TestMethod]
    public void BuildLink_Appends_BibId_When_No_Placeholder_Is_Present()
    {
        var options = OptionsFactory.Create(new BibliographicRecordLinkOptions
        {
            UrlTemplate = "https://example.test/bib/"
        });
        var builder = new BibliographicLinkBuilder(options);

        var result = builder.BuildLink(890);

        result.Should().Be("https://example.test/bib/890");
    }
}
