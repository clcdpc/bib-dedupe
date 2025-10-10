using System;
using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class DecisionBatchHistoryViewModel
{
    public IReadOnlyList<DecisionBatchHistory> Batches { get; init; } = Array.Empty<DecisionBatchHistory>();

    public bool HasBatches => Batches.Count > 0;
}
