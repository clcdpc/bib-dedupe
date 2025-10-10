using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionStore
{
    Task AddAsync(string userId, PairDecision decision);
    Task<PairDecision?> GetAsync(string userId, int leftBibId, int rightBibId);
    Task<IEnumerable<PairDecision>> GetAllAsync(string userId);
    Task RemoveAsync(string userId, int leftBibId, int rightBibId);
    Task UpdateAsync(string userId, PairDecision decision);
    Task<int> CountAsync(string userId);
}
