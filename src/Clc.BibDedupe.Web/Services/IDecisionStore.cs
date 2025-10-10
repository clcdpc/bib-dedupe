using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionStore
{
    Task AddAsync(string userId, DecisionItem decision);
    Task<DecisionItem?> GetAsync(string userId, int leftBibId, int rightBibId);
    Task<IEnumerable<DecisionItem>> GetAllAsync(string userId);
    Task RemoveAsync(string userId, int leftBibId, int rightBibId);
    Task UpdateAsync(string userId, DecisionItem decision);
    Task<int> CountAsync(string userId);
}
