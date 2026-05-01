using Clc.BibDedupe.Web.Models;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Clc.BibDedupe.Web.Services;

public class DecisionSubmissionService(
    IDecisionStore store,
    IDecisionBatchTracker tracker,
    IDecisionProcessingExecutor executor,
    IBackgroundJobClient backgroundJobs,
    ILogger<DecisionSubmissionService> logger) : IDecisionSubmissionService
{
    public Task<DecisionBatchStatus?> GetCurrentBatchAsync(string userEmail) => tracker.GetCurrentAsync(userEmail);

    public async Task<DecisionSubmissionResult> SubmitAsync(string userEmail)
    {
        var current = await tracker.GetCurrentAsync(userEmail);

        if (current is not null)
        {
            return DecisionSubmissionResult.AlreadyInProgress(current);
        }

        if (!await executor.CanProcessAsync())
        {
            logger.LogWarning("Decision processing is not available for {UserEmail}", userEmail);
            return DecisionSubmissionResult.ProcessingUnavailable();
        }

        var count = await store.CountAsync(userEmail);
        if (count == 0)
        {
            return DecisionSubmissionResult.NoDecisions();
        }

        var startedAt = DateTimeOffset.UtcNow;
        DecisionBatchStatus pendingBatch;
        try
        {
            pendingBatch = await tracker.StartAsync(userEmail, startedAt);
        }
        catch (ActiveDecisionBatchExistsException)
        {
            var activeBatch = await tracker.GetCurrentAsync(userEmail);
            return activeBatch is not null
                ? DecisionSubmissionResult.AlreadyInProgress(activeBatch)
                : DecisionSubmissionResult.AlreadyInProgress(new DecisionBatchStatus { JobId = string.Empty, StartedAt = startedAt });
        }
        var jobId = backgroundJobs.Enqueue<DecisionProcessingJob>(job => job.ExecuteAsync(userEmail));
        var status = await tracker.SetJobIdAsync(userEmail, pendingBatch.StartedAt, jobId);
        logger.LogInformation("Queued decision processing job {JobId} for {UserEmail}", jobId, userEmail);

        return DecisionSubmissionResult.Started(status);
    }
}
