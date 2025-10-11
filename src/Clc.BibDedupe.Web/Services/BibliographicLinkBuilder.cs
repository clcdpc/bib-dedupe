using System;
using System.Globalization;
using Clc.BibDedupe.Web.Options;
using Microsoft.Extensions.Options;

namespace Clc.BibDedupe.Web.Services;

public class BibliographicLinkBuilder : IBibliographicLinkBuilder
{
    private readonly IOptions<BibliographicRecordLinkOptions> options;

    public BibliographicLinkBuilder(IOptions<BibliographicRecordLinkOptions> options)
    {
        this.options = options;
    }

    public string? BuildLink(int bibId)
    {
        var template = options.Value.UrlTemplate;

        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var bibIdValue = bibId.ToString(CultureInfo.InvariantCulture);

        if (template.Contains(BibliographicRecordLinkOptions.Placeholder, StringComparison.Ordinal))
        {
            return template.Replace(BibliographicRecordLinkOptions.Placeholder, bibIdValue, StringComparison.Ordinal);
        }

        if (template.Contains("{0}", StringComparison.Ordinal))
        {
            return string.Format(CultureInfo.InvariantCulture, template, bibId);
        }

        return string.Concat(template, bibIdValue);
    }
}
