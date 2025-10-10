using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionBatchHistoryService
{
    Task<IReadOnlyList<DecisionBatchHistory>> GetHistoryAsync(string userEmail);
}
