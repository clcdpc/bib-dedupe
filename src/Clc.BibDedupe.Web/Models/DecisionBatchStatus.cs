namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchStatus
{
    public int BatchId { get; init; }
    public required string JobId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? FailedAt { get; init; }
    public string? FailureMessage { get; init; }

    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsFailed => FailedAt.HasValue || !string.IsNullOrWhiteSpace(FailureMessage);
    public bool IsTerminal => IsCompleted || IsFailed;
}
