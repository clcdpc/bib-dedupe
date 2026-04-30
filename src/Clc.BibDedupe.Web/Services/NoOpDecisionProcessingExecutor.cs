using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class NoOpDecisionProcessingExecutor : IDecisionProcessingExecutor
{
    public Task<bool> CanProcessAsync() => Task.FromResult(false);

    public Task<DecisionProcessingSummary> ExecuteAsync(string userEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DecisionProcessingSummary
        {
            TotalDecisions = 0,
            SucceededCount = 0,
            FailedCount = 0
        });
}
