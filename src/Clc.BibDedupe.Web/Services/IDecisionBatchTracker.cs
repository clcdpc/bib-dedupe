using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionBatchTracker
{
    Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail);
    Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt, string jobId);
    Task CompleteAsync(string userEmail, DateTimeOffset completedAt);
}
