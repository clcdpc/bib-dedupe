using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public class DecisionBatchListViewModel
{
    public IReadOnlyList<DecisionBatchSummary> Batches { get; init; } = new List<DecisionBatchSummary>();
    public DecisionBatchStatus? CurrentBatch { get; init; }

    public bool HasBatches => Batches.Count > 0;
    public bool HasFailures => Batches.Any(b => b.HasFailures);
    public int? CurrentBatchId => CurrentBatch?.BatchId;
}
