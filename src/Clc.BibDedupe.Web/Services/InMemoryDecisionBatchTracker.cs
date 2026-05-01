using System.Collections.Concurrent;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryDecisionBatchTracker : IDecisionBatchTracker
{
    private readonly ConcurrentDictionary<string, DecisionBatchStatus> batches = new();

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
        if (batches.TryGetValue(userEmail, out var existing) && !existing.IsTerminal)
        {
            throw new ActiveDecisionBatchExistsException(userEmail);
        }

        var status = new DecisionBatchStatus
        {
            JobId = string.Empty,
            StartedAt = startedAt,
            CompletedAt = null,
            FailedAt = null,
            FailureMessage = null
        };

        batches[userEmail] = status;
        return Task.FromResult(status);
    }

    public Task<DecisionBatchStatus> SetJobIdAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        if (!batches.TryGetValue(userEmail, out var status) || status.IsTerminal || status.StartedAt != startedAt)
        {
            throw new InvalidOperationException($"Unable to set JobId for active batch for {userEmail}.");
        }

        var updated = status with { JobId = jobId };
        batches[userEmail] = updated;
        return Task.FromResult(updated);
    }
}
