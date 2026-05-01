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
            var completedAt = DateTimeOffset.Now;
            await tracker.CompleteAsync(userEmail, completedAt);
            await TryNotifyCompletedAsync(userEmail, summary, completedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process decision batch for {UserEmail}", userEmail);

            var failedAt = DateTimeOffset.Now;
            var failureMessage = ex.ToString();
            if (failureMessage.Length > 1024)
            {
                failureMessage = failureMessage[..1024];
            }

            await tracker.FailAsync(userEmail, failedAt, failureMessage);
            await TryNotifyFailedAsync(userEmail, summary, failedAt, failureMessage);
            throw;
        }
    }

    private async Task TryNotifyCompletedAsync(string userEmail, DecisionProcessingSummary summary, DateTimeOffset completedAt)
    {
        try
        {
            await notificationService.NotifyCompletedAsync(userEmail, summary, completedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send completion notification for {UserEmail}", userEmail);
        }
    }

    private async Task TryNotifyFailedAsync(string userEmail, DecisionProcessingSummary? summary, DateTimeOffset failedAt, string failureMessage)
    {
        try
        {
            await notificationService.NotifyFailedAsync(userEmail, summary, failedAt, failureMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send failure notification for {UserEmail}", userEmail);
        }
    }
}
