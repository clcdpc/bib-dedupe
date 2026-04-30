using Clc.BibDedupe.Web.Models;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Clc.BibDedupe.Web.Services;

public class DecisionProcessingJob(
    IDecisionProcessingExecutor executor,
    IDecisionBatchTracker tracker,
    IDecisionBatchNotificationService notificationService,
    ILogger<DecisionProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(string userEmail)
    {
        DecisionProcessingSummary? summary = null;

        try
        {
            summary = await executor.ExecuteAsync(userEmail);
            var completedAt = DateTimeOffset.UtcNow;
            await tracker.CompleteAsync(userEmail, completedAt);
            await notificationService.NotifyCompletedAsync(userEmail, summary, completedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process decision batch for {UserEmail}", userEmail);

            var failedAt = DateTimeOffset.UtcNow;
            var failureMessage = ex.ToString();
            if (failureMessage.Length > 1024)
            {
                failureMessage = failureMessage[..1024];
            }

            await tracker.FailAsync(userEmail, failedAt, failureMessage);
            await notificationService.NotifyFailedAsync(userEmail, summary?.TotalDecisions ?? 0, failedAt, failureMessage);
            throw;
        }
    }
}
