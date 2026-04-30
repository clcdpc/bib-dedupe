using System.Threading;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionProcessingExecutor
{
    Task<bool> CanProcessAsync();
    Task<DecisionProcessingSummary> ExecuteAsync(string userEmail, CancellationToken cancellationToken = default);
}
