using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public record DecisionSummary
{
    public int Total { get; init; }
    public int KeepLeft { get; init; }
    public int KeepRight { get; init; }
    public int KeepBoth { get; init; }
    public int Skip { get; init; }

    public static DecisionSummary From(IEnumerable<DecisionItem> decisions)
    {
        var items = decisions?.ToList() ?? new List<DecisionItem>();

        if (items.Count == 0)
        {
            return new DecisionSummary();
        }

        var counts = items
            .GroupBy(d => d.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        return new DecisionSummary
        {
            Total = items.Count,
            KeepLeft = counts.GetValueOrDefault(BibDupePairAction.KeepLeft),
            KeepRight = counts.GetValueOrDefault(BibDupePairAction.KeepRight),
            KeepBoth = counts.GetValueOrDefault(BibDupePairAction.KeepBoth),
            Skip = counts.GetValueOrDefault(BibDupePairAction.Skip)
        };
    }
}
