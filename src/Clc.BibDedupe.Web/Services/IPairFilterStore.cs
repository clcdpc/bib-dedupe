using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IPairFilterStore
{
    Task<PairFilterOptions?> GetAsync(string userId);
    Task SetAsync(string userId, PairFilterOptions? filters);
}
