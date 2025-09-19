using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public record DecisionItem
{
    public int LeftBibId { get; init; }
    public int RightBibId { get; init; }
    public List<PairMatch> Matches { get; set; } = new();
    public int PrimaryMarcTomId { get; set; }
    public BibDupePairAction Action { get; set; }

    public string MatchSummary => Matches.Count == 0
        ? string.Empty
        : string.Join(", ", Matches.Select(m => $"{m.MatchType}: {m.MatchValue}"));
}
