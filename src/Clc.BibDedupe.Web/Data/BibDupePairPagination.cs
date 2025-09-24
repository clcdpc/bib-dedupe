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

        var grouped = pairs
            .GroupBy(p => p.PrimaryMarcTomId)
            .OrderBy(g => g.Min(p => p.PairId))
            .ThenBy(g => g.Key)
            .ToList();

        var groupPages = new Dictionary<int, int>();
        var runningTotal = 0;
        var currentPage = 1;

        foreach (var group in grouped)
        {
            var groupCount = group.Count();
            if (runningTotal > 0 && runningTotal + groupCount > normalizedPageSize)
            {
                currentPage++;
                runningTotal = 0;
            }

            groupPages[group.Key] = currentPage;
            runningTotal += groupCount;
        }

        var totalPages = groupPages.Count == 0
            ? 0
            : groupPages.Values.Max();

        var items = grouped
            .Where(g => groupPages[g.Key] == normalizedPage)
            .SelectMany(g => g.OrderBy(p => p.PairId))
            .ToList();

        return (items, total, totalPages);
    }
}
