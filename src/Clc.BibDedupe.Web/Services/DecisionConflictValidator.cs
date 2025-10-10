using System.Collections.Generic;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public static class DecisionConflictValidator
{
    public static void EnsureNoMergeConflicts(IEnumerable<DecisionItem> items)
    {
        var mergedBibIds = new HashSet<int>();
        var keptBibIds = new HashSet<int>();

        foreach (var item in items)
        {
            if (!IsMergeAction(item.Action))
            {
                continue;
            }

            var keepLeft = item.Action == BibDupePairAction.KeepLeft;
            var keptBibId = keepLeft ? item.Pair.LeftBibId : item.Pair.RightBibId;
            var mergedBibId = keepLeft ? item.Pair.RightBibId : item.Pair.LeftBibId;

            if (mergedBibIds.Contains(mergedBibId))
            {
                throw new DecisionConflictException(
                    mergedBibId,
                    $"Bib {mergedBibId} has already been selected to merge into another record. Remove the other merge before merging this bib again.");
            }

            if (keptBibIds.Contains(mergedBibId))
            {
                throw new DecisionConflictException(
                    mergedBibId,
                    $"Bib {mergedBibId} is already the kept record in another merge decision and cannot be merged into a different record.");
            }

            if (mergedBibIds.Contains(keptBibId))
            {
                throw new DecisionConflictException(
                    keptBibId,
                    $"Bib {keptBibId} has already been merged into a different record and cannot be the kept record in another merge decision.");
            }

            mergedBibIds.Add(mergedBibId);
            keptBibIds.Add(keptBibId);
        }
    }

    private static bool IsMergeAction(BibDupePairAction action) =>
        action is BibDupePairAction.KeepLeft or BibDupePairAction.KeepRight;
}
