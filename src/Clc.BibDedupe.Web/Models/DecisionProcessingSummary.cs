namespace Clc.BibDedupe.Web.Models;

public record DecisionProcessingSummary
{
    public required int TotalDecisions { get; init; }
    public required int SucceededCount { get; init; }
    public required int FailedCount { get; init; }
}
