using System.Linq;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class NextPairResolver(IBibDupePairRepository repository, IPairFilterStore pairFilterStore) : INextPairResolver
{
    public async Task<BibDupePair?> GetNextPairForUserAsync(
        string userEmail,
        PairFilterOptions? filters,
        (int leftBibId, int rightBibId)? excludePair = null)
    {
        var activeFilters = filters ?? await pairFilterStore.GetAsync(userEmail);

        var candidates = await repository.GetAsync(
            userEmail,
            activeFilters?.TomId,
            activeFilters?.MatchType,
            activeFilters?.HasHolds);

        return excludePair is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(p => p.LeftBibId != excludePair.Value.leftBibId || p.RightBibId != excludePair.Value.rightBibId);
    }
}
