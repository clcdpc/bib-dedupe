using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionSubmissionService
{
    Task<DecisionBatchStatus?> GetCurrentBatchAsync(string userEmail);
    Task<DecisionSubmissionResult> SubmitAsync(string userEmail);
}
