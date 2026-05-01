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
        var status = new DecisionBatchStatus
        {
            JobId = string.Empty,
            StartedAt = startedAt
        };

        if (!batches.TryAdd(userEmail, status) && batches.TryGetValue(userEmail, out var current) && !current.IsTerminal)
        {
            throw new InvalidOperationException($"An active decision batch already exists for {userEmail}.");
        }

        batches[userEmail] = status;
        return Task.FromResult(status);
    }

    public Task<DecisionBatchStatus> SetJobIdAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        if (!batches.TryGetValue(userEmail, out var status) || status.IsTerminal || status.StartedAt != startedAt)
        {
            throw new InvalidOperationException($"No active decision batch found for {userEmail} at {startedAt:o}.");
        }

        var updated = status with { JobId = jobId };
        batches[userEmail] = updated;
        return Task.FromResult(updated);
    }
}
