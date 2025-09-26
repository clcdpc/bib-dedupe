using System.Threading;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionProcessingExecutor
{
    Task<bool> CanProcessAsync();
    Task ExecuteAsync(string userEmail, CancellationToken cancellationToken = default);
}
