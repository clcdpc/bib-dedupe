using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize]
public class PairsController(IBibDupePairRepository repository, IDecisionStore decisionStore) : Controller
{
    private readonly IBibDupePairRepository _repository = repository;
    private readonly IDecisionStore _decisionStore = decisionStore;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var (items, total) = await _repository.GetPagedAsync(page, pageSize);
        var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var decidedPairs = (await _decisionStore.GetAllAsync(email))
            .Select(d => (d.LeftBibId, d.RightBibId))
            .ToHashSet();
        var filteredItems = items
            .Where(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)))
            .ToList();
        var model = new PairsListViewModel
        {
            Items = filteredItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = total - decidedPairs.Count
        };
        return View(model);
    }
}
