using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchDetail
{
    public required DecisionBatchSummary Summary { get; init; }
    public required IReadOnlyList<DecisionBatchResult> Results { get; init; }

    public bool HasResults => Results.Count > 0;
    public bool HasFailures => Results.Any(r => !r.Succeeded);
}
