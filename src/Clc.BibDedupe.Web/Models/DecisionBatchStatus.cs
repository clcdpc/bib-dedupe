namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchStatus
{
    public required string JobId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }

    public bool IsCompleted => CompletedAt.HasValue;
}
