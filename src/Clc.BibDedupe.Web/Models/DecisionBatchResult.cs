namespace Clc.BibDedupe.Web.Models;

public record DecisionBatchResult
{
    public required int LeftBibId { get; init; }
    public required int RightBibId { get; init; }
    public required BibDupePairAction Action { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
}
