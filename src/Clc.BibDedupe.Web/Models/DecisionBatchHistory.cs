using System;
using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchHistory
{
    public required int BatchId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? FailedAt { get; init; }
    public string? FailureMessage { get; init; }
    public IReadOnlyList<DecisionBatchResult> Results { get; init; } = Array.Empty<DecisionBatchResult>();

    public int TotalResults => Results.Count;
    public int SuccessCount => Results.Count(r => r.Succeeded);
    public int FailureCount => Results.Count(r => !r.Succeeded);
    public bool HasResults => TotalResults > 0;
    public bool HasFailures => FailureCount > 0;
    public bool HasFailed => FailedAt.HasValue || !string.IsNullOrWhiteSpace(FailureMessage);
}

public record DecisionBatchResult
{
    public required int LeftBibId { get; init; }
    public required int RightBibId { get; init; }
    public required BibDupePairAction Action { get; init; }
    public required bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
