using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository, IDecisionStore decisionStore) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var email = User.GetEmail();
        var decidedPairs = (await decisionStore.GetAllAsync(email))
            .Select(d => (d.LeftBibId, d.RightBibId))
            .ToHashSet();

        var allItems = (await repository.GetAsync())
            .Where(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)))
            .ToList();

        var pages = BuildPages(allItems, pageSize);
        var totalPages = pages.Count == 0 ? 1 : pages.Count;
        var currentPage = Math.Min(page, totalPages);
        var pageItems = pages.Count == 0 ? new List<BibDupePair>() : pages[currentPage - 1];

        var model = new PairsListViewModel
        {
            Items = pageItems,
            Page = currentPage,
            PageSize = pageSize,
            TotalCount = allItems.Count,
            TotalPages = totalPages
        };

        return View(model);
    }

    private static List<List<BibDupePair>> BuildPages(IEnumerable<BibDupePair> items, int pageSize)
    {
        var pages = new List<List<BibDupePair>>();
        var groupedItems = items
            .GroupBy(p => p.LeftBibId)
            .ToList();

        if (groupedItems.Count == 0)
        {
            return pages;
        }

        var currentPageItems = new List<BibDupePair>();

        foreach (var group in groupedItems)
        {
            var groupItems = group.ToList();

            if (currentPageItems.Count > 0 && currentPageItems.Count + groupItems.Count > pageSize)
            {
                pages.Add(currentPageItems);
                currentPageItems = new List<BibDupePair>();
            }

            currentPageItems.AddRange(groupItems);
        }

        if (currentPageItems.Count > 0)
        {
            pages.Add(currentPageItems);
        }

        return pages;
    }
}
