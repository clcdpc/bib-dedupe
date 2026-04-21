using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface INextPairResolver
{
    Task<BibDupePair?> GetNextPairForUserAsync(
        string userEmail,
        PairFilterOptions? filters,
        (int leftBibId, int rightBibId)? excludePair = null);
}
