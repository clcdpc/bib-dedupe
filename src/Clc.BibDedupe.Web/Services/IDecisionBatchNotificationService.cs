using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionBatchNotificationService
{
    Task NotifyCompletedAsync(string userEmail, DecisionProcessingSummary summary, DateTimeOffset completedAt);
    Task NotifyFailedAsync(string userEmail, int totalDecisions, DateTimeOffset failedAt, string failureMessage);
}
