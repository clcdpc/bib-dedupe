using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public class DecisionIndexViewModel
{
    public IReadOnlyList<DecisionItem> Decisions { get; init; } = new List<DecisionItem>();
    public DecisionBatchStatus? BatchStatus { get; init; }
    public DecisionSummary Summary { get; init; } = new();

    public bool HasDecisions => Decisions.Count > 0;
    public bool HasPendingBatch => BatchStatus is not null && !BatchStatus.IsCompleted;

    public static DecisionIndexViewModel Create(IEnumerable<DecisionItem> decisions, DecisionBatchStatus? batch)
    {
        var items = decisions?.ToList() ?? new List<DecisionItem>();

        return new DecisionIndexViewModel
        {
            Decisions = items,
            BatchStatus = batch,
            Summary = DecisionSummary.From(items)
        };
    }
}
