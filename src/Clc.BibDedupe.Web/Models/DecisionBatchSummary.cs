namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchSummary
{
    public required int BatchId { get; init; }
    public required string JobId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }

    public bool IsCompleted => CompletedAt.HasValue;
    public bool HasFailures => FailureCount > 0;
}
