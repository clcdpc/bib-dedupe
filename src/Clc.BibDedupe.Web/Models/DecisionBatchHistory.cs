using System;
using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchHistory
{
    private readonly IReadOnlyList<DecisionBatchResult> _results = Array.Empty<DecisionBatchResult>();

    public required int BatchId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? FailedAt { get; init; }
    public string? FailureMessage { get; init; }

    public IReadOnlyList<DecisionBatchResult> Results
    {
        get => _results;
        init
        {
            _results = value;
            SuccessCount = _results.Count(r => r.Succeeded);
            FailureCount = _results.Count(r => !r.Succeeded);
        }
    }

    public int TotalResults => Results.Count;
    public int SuccessCount { get; private init; }
    public int FailureCount { get; private init; }
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
