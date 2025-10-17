using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models
{
    public class IndexViewModel
    {
        public int LeftBibId { get; set; }
        public int RightBibId { get; set; }
        public string? LeftTitle { get; set; }
        public string? RightTitle { get; set; }
        public string LeftBibXml { get; set; } = string.Empty;
        public string RightBibXml { get; set; } = string.Empty;
        public List<Dictionary<string, string>> LeftItems { get; set; } = new();
        public List<Dictionary<string, string>> RightItems { get; set; } = new();
        public List<PairMatch> Matches { get; set; } = new();
        public int LeftHoldCount { get; set; }
        public int RightHoldCount { get; set; }
        public int TotalHoldCount { get; set; }
        public HashSet<BibDupePairAction> ValidActions { get; set; } = new();
        public List<ItemField> ItemFields { get; set; } = new()
        {
            new("AssignedBranch", "Assigned Branch"),
            new("Collection", "Collection"),
            new("MaterialType", "Material Type"),
            new("ShelfLocation", "Shelf Location"),
            new("CallNumber", "Call #"),
            new("CircStatus", "Status"),
            new("Barcode", "Barcode")
        };
    }
}
