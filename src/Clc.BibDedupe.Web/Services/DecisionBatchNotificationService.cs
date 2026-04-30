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

        var body = BuildSummaryBody("completed", completedAt, summary, includeError: false, errorMessage: null);
        await emailSender.SendAsync(userEmail, "Decision batch completed", body);
    }

    public async Task NotifyFailedAsync(string userEmail, DecisionProcessingSummary summary, DateTimeOffset failedAt, string failureMessage)
    {
        if (!CanSend())
        {
            return;
        }

        var body = BuildSummaryBody("failed", failedAt, summary, includeError: true, errorMessage: failureMessage);
        await emailSender.SendAsync(userEmail, "Decision batch failed", body);
    }

    private static string BuildSummaryBody(string status, DateTimeOffset timestamp, DecisionProcessingSummary summary, bool includeError, string? errorMessage)
    {
        var body = $"""
Decision batch processing {status} at {timestamp:O}.

Summary:
- Total decisions: {summary.TotalDecisions}
- Succeeded: {summary.SucceededCount}
- Failed: {summary.FailedCount}
""";

        if (includeError)
        {
            body += $"""

Error:
{errorMessage}
""";
        }

        return body;
    }

    private bool CanSend() => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.SenderEmail);
}
