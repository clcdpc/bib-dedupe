using Microsoft.Extensions.Logging;

namespace Clc.BibDedupe.Web.Services;

public class DecisionProcessingJob(
    IDecisionProcessingExecutor executor,
    IDecisionBatchTracker tracker,
    ILogger<DecisionProcessingJob> logger)
{
    public async Task ExecuteAsync(string userEmail)
    {
        try
        {
            await executor.ExecuteAsync(userEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process decision batch for {UserEmail}", userEmail);
            throw;
        }
        finally
        {
            await tracker.CompleteAsync(userEmail, DateTimeOffset.UtcNow);
        }
    }
}
