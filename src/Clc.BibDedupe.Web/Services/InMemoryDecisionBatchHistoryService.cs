using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryDecisionBatchHistoryService : IDecisionBatchHistoryService
{
    public Task<IReadOnlyList<DecisionBatchHistory>> GetHistoryAsync(string userEmail) =>
        Task.FromResult<IReadOnlyList<DecisionBatchHistory>>(Array.Empty<DecisionBatchHistory>());
}
