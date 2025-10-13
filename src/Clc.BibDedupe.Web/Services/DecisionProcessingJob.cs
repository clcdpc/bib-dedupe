using Hangfire;
using Microsoft.Extensions.Logging;

namespace Clc.BibDedupe.Web.Services;

public class DecisionProcessingJob(
    IDecisionProcessingExecutor executor,
    IDecisionBatchTracker tracker,
    ILogger<DecisionProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(string userEmail)
    {
        try
        {
            await executor.ExecuteAsync(userEmail);
            await tracker.CompleteAsync(userEmail, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process decision batch for {UserEmail}", userEmail);

            var failureMessage = ex.ToString();
            if (failureMessage.Length > 1024)
            {
                failureMessage = failureMessage[..1024];
            }

            await tracker.FailAsync(userEmail, DateTimeOffset.UtcNow, failureMessage);
            throw;
        }
    }
}
