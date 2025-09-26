namespace Clc.BibDedupe.Web.Models;

public class DecisionSubmissionResult
{
    private DecisionSubmissionResult()
    {
    }

    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public DecisionBatchStatus? BatchStatus { get; private init; }

    public static DecisionSubmissionResult Started(DecisionBatchStatus status) => new()
    {
        Success = true,
        BatchStatus = status
    };

    public static DecisionSubmissionResult AlreadyInProgress(DecisionBatchStatus status) => new()
    {
        Success = false,
        BatchStatus = status,
        ErrorMessage = "A batch is already being processed."
    };

    public static DecisionSubmissionResult NoDecisions() => new()
    {
        Success = false,
        ErrorMessage = "There are no decisions to submit."
    };

    public static DecisionSubmissionResult ProcessingUnavailable() => new()
    {
        Success = false,
        ErrorMessage = "Decision processing is not available."
    };
}
