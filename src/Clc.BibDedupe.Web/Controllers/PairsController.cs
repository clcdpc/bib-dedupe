using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository, IDecisionStore decisionStore) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var (items, total) = await repository.GetPagedAsync(page, pageSize);
        var email = User.GetEmail();
        var decidedPairs = (await decisionStore.GetAllAsync(email))
            .Select(d => (d.LeftBibId, d.RightBibId))
            .ToHashSet();
        var filteredItems = items
            .Where(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)))
            .ToList();
        var effectivePageSize = Math.Max(pageSize, filteredItems.Count);
        var model = new PairsListViewModel
        {
            Items = filteredItems,
            Page = page,
            PageSize = effectivePageSize,
            TotalCount = total - decidedPairs.Count
        };
        return View(model);
    }
}
