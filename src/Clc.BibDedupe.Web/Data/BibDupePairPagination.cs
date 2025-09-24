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

        var groupPages = new List<(int Page, List<BibDupePair> Pairs)>();
        var currentPage = 1;
        var currentCount = 0;

        foreach (var group in orderedGroups)
        {
            var groupPairs = group.Pairs;
            var groupCount = groupPairs.Count;

            if (groupCount > normalizedPageSize)
            {
                if (currentCount > 0)
                {
                    currentPage++;
                    currentCount = 0;
                }

                var index = 0;
                while (index < groupCount)
                {
                    var remaining = groupCount - index;
                    var take = Math.Min(normalizedPageSize, remaining);
                    var chunk = groupPairs.GetRange(index, take);
                    groupPages.Add((currentPage, chunk));
                    index += take;

                    if (take == normalizedPageSize)
                    {
                        currentPage++;
                        currentCount = 0;
                    }
                    else
                    {
                        currentCount = take;
                    }
                }

                if (currentCount >= normalizedPageSize)
                {
                    currentPage++;
                    currentCount = 0;
                }

                continue;
            }

            if (currentCount > 0 && currentCount + groupCount > normalizedPageSize)
            {
                currentPage++;
                currentCount = 0;
            }

            groupPages.Add((currentPage, groupPairs));

            currentCount += groupCount;
            if (currentCount >= normalizedPageSize)
            {
                currentPage++;
                currentCount = 0;
            }
        }

        if (groupPages.Count == 0)
        {
            return (Array.Empty<BibDupePair>(), total, 0);
        }

        var totalPages = groupPages[^1].Page;
        var clampedPage = Math.Min(normalizedPage, totalPages);
        var items = groupPages
            .Where(g => g.Page == clampedPage)
            .SelectMany(g => g.Pairs)
            .ToList();

        return (items, total, totalPages);
    }
}
