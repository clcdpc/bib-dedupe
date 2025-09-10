using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data
{
    public interface IBibDupePairRepository
    {
        Task<IEnumerable<BibDupePair>> GetAsync();
        Task<(IEnumerable<BibDupePair> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action);
        Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail);
        Task SkipAsync(int leftBibId, int rightBibId, string userEmail);
    }
}
