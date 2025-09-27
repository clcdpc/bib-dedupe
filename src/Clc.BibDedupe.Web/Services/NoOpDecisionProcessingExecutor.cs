namespace Clc.BibDedupe.Web.Services;

public class NoOpDecisionProcessingExecutor : IDecisionProcessingExecutor
{
    public Task<bool> CanProcessAsync() => Task.FromResult(false);

    public Task ExecuteAsync(string userEmail, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
