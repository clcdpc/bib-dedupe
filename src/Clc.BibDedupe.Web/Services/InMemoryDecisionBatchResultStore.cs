using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryDecisionBatchResultStore : IDecisionBatchResultStore
{
    public Task<DecisionBatchDetail?> GetDetailAsync(string userEmail, int batchId) =>
        Task.FromResult<DecisionBatchDetail?>(null);

    public Task<IReadOnlyList<DecisionBatchSummary>> GetSummariesAsync(string userEmail) =>
        Task.FromResult<IReadOnlyList<DecisionBatchSummary>>(new List<DecisionBatchSummary>());
}
