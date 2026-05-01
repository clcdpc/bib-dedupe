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
    private static readonly TimeSpan PendingBatchStaleThreshold = TimeSpan.FromMinutes(5);

    public async Task<DecisionBatchStatus?> GetCurrentBatchAsync(string userEmail)
    {
        await tracker.FailOrphanedPendingAsync(
            DateTimeOffset.UtcNow.Subtract(PendingBatchStaleThreshold),
            "Decision processing job was not enqueued.");

        return await tracker.GetCurrentAsync(userEmail);
    }

    public async Task<DecisionSubmissionResult> SubmitAsync(string userEmail)
    {
        await tracker.FailOrphanedPendingAsync(
            DateTimeOffset.UtcNow.Subtract(PendingBatchStaleThreshold),
            "Decision processing job was not enqueued.");

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
        string jobId;
        try
        {
            jobId = backgroundJobs.Enqueue<DecisionProcessingJob>(job => job.ExecuteAsync(userEmail));
        }
        catch (Exception ex)
        {
            await tracker.FailAsync(userEmail, DateTimeOffset.UtcNow, "Failed to enqueue decision processing job.");
            logger.LogError(ex, "Failed to enqueue decision processing job for {UserEmail}", userEmail);
            return DecisionSubmissionResult.ProcessingUnavailable();
        }
        if (string.IsNullOrWhiteSpace(jobId))
        {
            await tracker.FailAsync(userEmail, DateTimeOffset.UtcNow, "Decision processing job enqueue was cancelled.");
            logger.LogWarning("Decision processing job enqueue was cancelled for {UserEmail}", userEmail);
            return DecisionSubmissionResult.ProcessingUnavailable();
        }

        var status = await tracker.SetJobIdAsync(pendingBatch.BatchId, jobId);
        logger.LogInformation("Queued decision processing job {JobId} for {UserEmail}", jobId, userEmail);

        return DecisionSubmissionResult.Started(status);
    }
}
