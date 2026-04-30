using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Options;
using Microsoft.Extensions.Options;

namespace Clc.BibDedupe.Web.Services;

public class DecisionBatchNotificationService(
    IEmailSender emailSender,
    IOptions<DecisionBatchNotificationOptions> options) : IDecisionBatchNotificationService
{
    public async Task NotifyCompletedAsync(string userEmail, DecisionProcessingSummary summary, DateTimeOffset completedAt)
    {
        if (!CanSend())
        {
            return;
        }

        var body = $"""
Decision batch processing completed at {completedAt:O}.

Summary:
- Total decisions: {summary.TotalDecisions}
- Succeeded: {summary.SucceededCount}
- Failed: {summary.FailedCount}
""";

        await emailSender.SendAsync(userEmail, "Decision batch completed", body);
    }

    public async Task NotifyFailedAsync(string userEmail, int totalDecisions, DateTimeOffset failedAt, string failureMessage)
    {
        if (!CanSend())
        {
            return;
        }

        var body = $"""
Decision batch processing failed at {failedAt:O}.

Summary:
- Total decisions: {totalDecisions}
- Succeeded: 0
- Failed: {totalDecisions}

Error:
{failureMessage}
""";

        await emailSender.SendAsync(userEmail, "Decision batch failed", body);
    }

    private bool CanSend() => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.SenderEmail);
}
