using System.Collections.Generic;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data
{
    public interface IBibDupePairRepository
    {
        Task<IEnumerable<BibDupePair>> GetAsync();
        Task KeepLeftAsync(int leftBibId, int rightBibId, string userEmail);
        Task KeepRightAsync(int leftBibId, int rightBibId, string userEmail);
        Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail);
        Task SkipAsync(int leftBibId, int rightBibId, string userEmail);
    }
}
