using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public class DecisionIndexViewModel : LayoutViewModel
{
    public IReadOnlyList<PairDecision> Decisions { get; init; } = new List<PairDecision>();
    public DecisionBatchStatus? BatchStatus { get; init; }
    public DecisionSummary Summary { get; init; } = new();

    public bool HasDecisions => Decisions.Count > 0;
    public bool HasPendingBatch => BatchStatus is not null && !BatchStatus.IsTerminal;

    public static DecisionIndexViewModel Create(IEnumerable<PairDecision> decisions, DecisionBatchStatus? batch)
    {
        var items = decisions?.ToList() ?? new List<PairDecision>();

        return new DecisionIndexViewModel
        {
            Decisions = items,
            BatchStatus = batch,
            Summary = DecisionSummary.From(items)
        };
    }
}
