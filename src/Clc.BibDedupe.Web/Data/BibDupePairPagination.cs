using System;
using System.Collections.Generic;
using System.Linq;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data;

internal static class BibDupePairPagination
{
    public static (IReadOnlyList<BibDupePair> Items, int TotalCount, int TotalPages) Paginate(
        IReadOnlyList<BibDupePair> pairs,
        int page,
        int pageSize)
    {
        var total = pairs.Count;
        if (total == 0)
        {
            return (Array.Empty<BibDupePair>(), 0, 0);
        }

        var normalizedPageSize = Math.Max(pageSize, 1);
        var normalizedPage = Math.Max(page, 1);

        var orderedGroups = pairs
            .GroupBy(p => p.PrimaryMarcTomId)
            .Select(g =>
            {
                var orderedPairs = g.OrderBy(p => p.PairId).ToList();
                return new
                {
                    Primary = g.Key,
                    Pairs = orderedPairs,
                    FirstPairId = orderedPairs.Count > 0 ? orderedPairs[0].PairId : 0
                };
            })
            .OrderBy(g => g.FirstPairId)
            .ThenBy(g => g.Primary)
            .ToList();

        var pages = new List<List<BibDupePair>>();
        var currentPagePairs = new List<BibDupePair>();
        var currentCount = 0;

        foreach (var group in orderedGroups)
        {
            var groupCount = group.Pairs.Count;
            if (currentCount > 0 && currentCount + groupCount > normalizedPageSize)
            {
                pages.Add(currentPagePairs);
                currentPagePairs = new List<BibDupePair>();
                currentCount = 0;
            }

            currentPagePairs.AddRange(group.Pairs);
            currentCount += groupCount;

            if (groupCount >= normalizedPageSize)
            {
                pages.Add(currentPagePairs);
                currentPagePairs = new List<BibDupePair>();
                currentCount = 0;
            }
        }

        if (currentCount > 0)
        {
            pages.Add(currentPagePairs);
        }

        var totalPages = pages.Count;
        if (totalPages == 0)
        {
            return (Array.Empty<BibDupePair>(), total, 0);
        }

        var clampedPage = Math.Min(normalizedPage, totalPages);
        var items = pages[clampedPage - 1];

        return (items, total, totalPages);
    }
}
