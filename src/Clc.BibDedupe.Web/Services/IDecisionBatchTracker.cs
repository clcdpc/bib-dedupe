using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionBatchTracker
{
    Task FailOrphanedPendingAsync(DateTimeOffset staleBefore, string failureMessage);
    Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail);
    Task<DecisionBatchStatus?> GetByBatchIdAsync(int batchId);
    Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt);
    Task<DecisionBatchStatus> SetJobIdAsync(int batchId, string jobId);
    Task CompleteAsync(string userEmail, DateTimeOffset completedAt);
    Task FailAsync(string userEmail, DateTimeOffset failedAt, string errorMessage);
}
