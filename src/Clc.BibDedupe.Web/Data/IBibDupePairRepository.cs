using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data
{
    public interface IBibDupePairRepository
    {
        Task<IEnumerable<BibDupePair>> GetAsync(
            string? userEmail = null,
            int? tomId = null,
            string? matchType = null,
            bool? hasHolds = null,
            bool hideDecided = true);
        Task<PairsPageResult> GetPagedAsync(
            int page,
            int pageSize,
            string? userEmail = null,
            int? tomId = null,
            string? matchType = null,
            bool? hasHolds = null,
            bool hideDecided = true);
        Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId, string? userEmail = null, bool hideDecided = true);
        Task<IReadOnlyCollection<BibDupePairAction>> GetValidActionsAsync(int leftBibId, int rightBibId, string userEmail);
        Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action);
        Task MarkNotDuplicateAsync(int leftBibId, int rightBibId, string userEmail);
        Task SkipAsync(int leftBibId, int rightBibId, string userEmail);
    }
}
