using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class ItemsTableViewModel
{
    public List<Dictionary<string, string>> Items { get; set; } = new();
    public List<ItemField> Fields { get; set; } = new();
}
