using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class RecordData
{
    public string BibXml { get; set; } = string.Empty;
    public List<Dictionary<string, string>> Items { get; set; } = new();
}
