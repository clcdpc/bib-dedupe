namespace Clc.BibDedupe.Web.Models;

public record DecisionItem
{
    public int LeftBibId { get; init; }
    public int RightBibId { get; init; }
    public string MatchType { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public int PrimaryMarcTomId { get; set; }
    public string LeftTitle { get; set; } = string.Empty;
    public string LeftAuthor { get; set; } = string.Empty;
    public string RightTitle { get; set; } = string.Empty;
    public string RightAuthor { get; set; } = string.Empty;
    public BibDupePairAction Action { get; set; }
}
