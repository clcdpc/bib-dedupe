namespace Clc.BibDedupe.Web.Options;

public class BibliographicRecordLinkOptions
{
    public const string ConfigurationKey = "LeapBibLinkFormat";
    public const string Placeholder = "{bibId}";

    public string? UrlTemplate { get; set; }
}
