using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface ICurrentPairStore
{
    Task<CurrentPair?> GetAsync(string userId);
    Task SetAsync(string userId, CurrentPair currentPair);
    Task ClearAsync(string userId);
}
