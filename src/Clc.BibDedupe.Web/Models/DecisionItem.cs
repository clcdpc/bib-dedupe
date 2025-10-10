namespace Clc.BibDedupe.Web.Models;

public record DecisionItem
{
    private BibDupePair _pair = new();

    public BibDupePair Pair
    {
        get => _pair;
        set => _pair = value ?? new BibDupePair();
    }
    public BibDupePairAction Action { get; set; }

    public int LeftBibId => Pair.LeftBibId;
    public int RightBibId => Pair.RightBibId;
    public string MatchSummary => Pair.MatchSummary;
}
