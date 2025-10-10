using System.Collections.Concurrent;
using System.Threading;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryDecisionBatchTracker : IDecisionBatchTracker
{
    private readonly ConcurrentDictionary<string, DecisionBatchStatus> batches = new();
    private int nextBatchId;

    public Task CompleteAsync(string userEmail, DateTimeOffset completedAt)
    {
        if (batches.TryGetValue(userEmail, out var status) && !status.IsCompleted)
        {
            batches[userEmail] = status with { CompletedAt = completedAt };
        }

        return Task.CompletedTask;
    }

    public Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail)
    {
        if (batches.TryGetValue(userEmail, out var status) && !status.IsCompleted)
        {
            return Task.FromResult<DecisionBatchStatus?>(status);
        }

        return Task.FromResult<DecisionBatchStatus?>(null);
    }

    public Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        var status = new DecisionBatchStatus
        {
            BatchId = Interlocked.Increment(ref nextBatchId),
            JobId = jobId,
            StartedAt = startedAt,
            CompletedAt = null
        };

        batches[userEmail] = status;
        return Task.FromResult(status);
    }
}
