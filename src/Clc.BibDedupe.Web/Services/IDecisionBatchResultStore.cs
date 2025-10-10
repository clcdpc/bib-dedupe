using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionBatchResultStore
{
    Task<IReadOnlyList<DecisionBatchSummary>> GetSummariesAsync(string userEmail);
    Task<DecisionBatchDetail?> GetDetailAsync(string userEmail, int batchId);
}
