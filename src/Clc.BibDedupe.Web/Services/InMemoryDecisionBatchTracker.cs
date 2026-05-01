using System.Collections.Concurrent;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryDecisionBatchTracker : IDecisionBatchTracker
{
    private readonly ConcurrentDictionary<string, DecisionBatchStatus> batches = new();
    private int nextBatchId;
    public Task FailOrphanedPendingAsync(DateTimeOffset staleBefore, string failureMessage)
    {
        foreach (var (userEmail, status) in batches)
        {
            if (!status.IsTerminal && string.IsNullOrWhiteSpace(status.JobId) && status.StartedAt <= staleBefore)
            {
                batches[userEmail] = status with { FailedAt = DateTimeOffset.UtcNow, FailureMessage = failureMessage };
            }
        }

        return Task.CompletedTask;
    }

    public Task CompleteAsync(string userEmail, DateTimeOffset completedAt)
    {
        if (batches.TryGetValue(userEmail, out var status) && !status.IsTerminal)
        {
            batches[userEmail] = status with { CompletedAt = completedAt, FailedAt = null, FailureMessage = null };
        }

        return Task.CompletedTask;
    }

    public Task FailAsync(string userEmail, DateTimeOffset failedAt, string errorMessage)
    {
        if (batches.TryGetValue(userEmail, out var status) && !status.IsTerminal)
        {
            batches[userEmail] = status with { FailedAt = failedAt, FailureMessage = errorMessage };
        }

        return Task.CompletedTask;
    }

    public Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail)
    {
        if (batches.TryGetValue(userEmail, out var status) && !status.IsTerminal)
        {
            return Task.FromResult<DecisionBatchStatus?>(status);
        }

        return Task.FromResult<DecisionBatchStatus?>(null);
    }

    public Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt)
    {
        var status = new DecisionBatchStatus
        {
            BatchId = Interlocked.Increment(ref nextBatchId),
            JobId = string.Empty,
            StartedAt = startedAt,
            CompletedAt = null,
            FailedAt = null,
            FailureMessage = null
        };

        try
        {
            var updated = batches.AddOrUpdate(
                userEmail,
                status,
                (_, existing) => existing.IsTerminal ? status : throw new ActiveDecisionBatchExistsException(userEmail));

            return Task.FromResult(updated);
        }
        catch (ActiveDecisionBatchExistsException)
        {
            throw;
        }
    }

    public Task<DecisionBatchStatus> SetJobIdAsync(int batchId, string jobId)
    {
        var batch = batches.FirstOrDefault(kvp => kvp.Value.BatchId == batchId);
        if (batch.Equals(default(KeyValuePair<string, DecisionBatchStatus>)))
        {
            throw new InvalidOperationException($"Unable to set JobId for batch {batchId}.");
        }

        var userEmail = batch.Key;
        var status = batch.Value;
        var updated = string.IsNullOrWhiteSpace(status.JobId)
            ? status with { JobId = jobId }
            : status;
        batches[userEmail] = updated;
        return Task.FromResult(updated);
    }
}
