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

        var chunkSize = Math.Max(pageSize * 2, 128);
        var (initialItems, totalPairs) = await repository.GetPagedAsync(1, chunkSize);
        var buffer = new Queue<BibDupePair>(initialItems);
        var fetched = buffer.Count;
        var moreData = fetched < totalPairs;

        var currentGroup = new List<BibDupePair>();
        var currentPageItems = new List<BibDupePair>();
        List<BibDupePair>? requestedPageItems = null;
        List<BibDupePair>? lastPageItems = null;
        var pageCaptured = false;
        var totalPages = 0;
        var totalUndecided = 0;

        async Task<bool> LoadNextChunkAsync()
        {
            if (!moreData)
            {
                return false;
            }

            var nextChunk = await repository.GetSegmentAsync(fetched, chunkSize);
            if (nextChunk.Count == 0)
            {
                moreData = false;
                return false;
            }

            foreach (var item in nextChunk)
            {
                buffer.Enqueue(item);
            }

            fetched += nextChunk.Count;
            moreData = fetched < totalPairs;
            return true;
        }

        void FinalizePage()
        {
            totalPages++;
            if (!pageCaptured && totalPages == page)
            {
                requestedPageItems = currentPageItems;
                pageCaptured = true;
            }

            lastPageItems = currentPageItems;
            currentPageItems = new List<BibDupePair>();
        }

        void ProcessGroup(List<BibDupePair> group)
        {
            var filtered = group
                .Where(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)))
                .ToList();

            if (filtered.Count == 0)
            {
                return;
            }

            totalUndecided += filtered.Count;

            if (currentPageItems.Count == 0)
            {
                currentPageItems.AddRange(filtered);
                if (currentPageItems.Count >= pageSize)
                {
                    FinalizePage();
                }

                return;
            }

            var projectedCount = currentPageItems.Count + filtered.Count;

            if (projectedCount < pageSize)
            {
                currentPageItems.AddRange(filtered);
                return;
            }

            if (projectedCount == pageSize)
            {
                currentPageItems.AddRange(filtered);
                FinalizePage();
                return;
            }

            if (currentPageItems.Count >= pageSize)
            {
                FinalizePage();
                currentPageItems.AddRange(filtered);
                if (currentPageItems.Count >= pageSize)
                {
                    FinalizePage();
                }

                return;
            }

            currentPageItems.AddRange(filtered);
            FinalizePage();
        }

        while (buffer.Count > 0 || moreData)
        {
            if (buffer.Count == 0)
            {
                var loaded = await LoadNextChunkAsync();
                if (!loaded && buffer.Count == 0)
                {
                    break;
                }
            }

            if (buffer.Count == 0)
            {
                break;
            }

            var pair = buffer.Dequeue();
            if (currentGroup.Count == 0 || currentGroup[0].LeftBibId == pair.LeftBibId)
            {
                currentGroup.Add(pair);
            }
            else
            {
                ProcessGroup(currentGroup);
                currentGroup = new List<BibDupePair> { pair };
            }
        }

        if (currentGroup.Count > 0)
        {
            ProcessGroup(currentGroup);
        }

        if (currentPageItems.Count > 0)
        {
            FinalizePage();
        }

        if (!pageCaptured)
        {
            requestedPageItems = lastPageItems ?? new List<BibDupePair>();
        }

        requestedPageItems ??= new List<BibDupePair>();

        var effectivePage = pageCaptured ? page : (totalPages > 0 ? totalPages : 1);
        var effectiveTotalPages = totalPages > 0 ? totalPages : (totalUndecided > 0 ? 1 : 0);

        var model = new PairsListViewModel
        {
            Items = requestedPageItems,
            Page = effectivePage,
            PageSize = pageSize,
            TotalCount = totalUndecided,
            TotalPages = effectiveTotalPages
        };

        return View(model);
    }
}
