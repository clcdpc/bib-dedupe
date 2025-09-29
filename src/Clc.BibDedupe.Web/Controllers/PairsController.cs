using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository, IDecisionStore decisionStore) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var allItems = (await repository.GetAsync()).ToList();

        var email = User.GetEmail();
        var decidedPairs = (await decisionStore.GetAllAsync(email))
            .Select(d => (d.LeftBibId, d.RightBibId))
            .ToHashSet();

        var filteredItems = allItems
            .Where(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)))
            .OrderBy(p => p.LeftBibId)
            .ThenBy(p => p.RightBibId)
            .ToList();

        var pageBoundaries = BuildPageBoundaries(filteredItems, pageSize);
        var totalPages = pageBoundaries.Count;
        var currentPage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

        List<BibDupePair> visiblePairs = new();
        if (totalPages > 0)
        {
            var (start, count) = pageBoundaries[currentPage - 1];
            visiblePairs = filteredItems.GetRange(start, count);
        }

        var model = new PairsListViewModel
        {
            Items = visiblePairs,
            Page = currentPage,
            PageSize = pageSize,
            TotalCount = filteredItems.Count,
            TotalPages = totalPages
        };

        return View(model);
    }

    private static List<(int Start, int Count)> BuildPageBoundaries(IReadOnlyList<BibDupePair> items, int pageSize)
    {
        var pages = new List<(int Start, int Count)>();

        if (items.Count == 0)
        {
            return pages;
        }

        var index = 0;
        while (index < items.Count)
        {
            var pageStart = index;
            var count = 0;

            while (index < items.Count && count < pageSize)
            {
                var leftBibId = items[index].LeftBibId;
                var groupStart = index;

                while (index < items.Count && items[index].LeftBibId == leftBibId)
                {
                    index++;
                }

                count += index - groupStart;
            }

            pages.Add((pageStart, index - pageStart));
        }

        return pages;
    }
}
