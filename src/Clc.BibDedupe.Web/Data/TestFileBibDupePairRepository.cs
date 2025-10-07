using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data;

public class TestFileBibDupePairRepository : IBibDupePairRepository
{
    private int _nextPairId;
    private int _nextBibId;
    private readonly List<BibDupePair> _pairs;

    public TestFileBibDupePairRepository(int pairCount = 10)
    {
        _pairs = GeneratePairs(pairCount).ToList();
    }

    private BibDupePair CreatePair()
    {
        var pairId = Interlocked.Increment(ref _nextPairId);
        var left = Interlocked.Increment(ref _nextBibId);
        var right = Interlocked.Increment(ref _nextBibId);
        var toms = new[]
        {
            new TomOption(1, "Book"),
            new TomOption(2, "Audio"),
            new TomOption(3, "Video")
        };
        var tom = toms[pairId % toms.Length];
        var leftHolds = pairId % 3;
        var rightHolds = pairId % 2;
        return new BibDupePair
        {
            PairId = pairId,
            LeftBibId = left,
            RightBibId = right,
            PrimaryMarcTomId = tom.Id,
            TOM = tom.Description,
            LeftTitle = $"Left Title {pairId}",
            LeftAuthor = $"Left Author {pairId}",
            RightTitle = $"Right Title {pairId}",
            RightAuthor = $"Right Author {pairId}",
            LeftHoldCount = leftHolds,
            RightHoldCount = rightHolds,
            TotalHoldCount = leftHolds + rightHolds,
            Matches = new List<PairMatch>
            {
                new()
                {
                    MatchType = "Test",
                    MatchValue = "Test"
                },
                new()
                {
                    MatchType = pairId % 2 == 0 ? "Secondary" : "Primary",
                    MatchValue = $"Value {pairId}"
                }
            }
        };
    }

    private IEnumerable<BibDupePair> GeneratePairs(int count)
        => Enumerable.Range(0, count).Select(_ => CreatePair());

    public Task<IEnumerable<BibDupePair>> GetAsync()
        => Task.FromResult<IEnumerable<BibDupePair>>(_pairs);

    public Task<PairsPageResult> GetPagedAsync(
        int page,
        int pageSize,
        string? userEmail = null,
        int? tomId = null,
        string? matchType = null,
        bool? hasHolds = null)
    {
        var requiresHold = hasHolds == true;

        var filtered = _pairs.AsEnumerable();

        if (tomId.HasValue)
        {
            filtered = filtered.Where(p => p.PrimaryMarcTomId == tomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(matchType))
        {
            filtered = filtered.Where(p => p.Matches.Any(m => string.Equals(m.MatchType, matchType, StringComparison.OrdinalIgnoreCase)));
        }

        if (requiresHold)
        {
            filtered = filtered.Where(p => p.LeftHoldCount > 0 || p.RightHoldCount > 0);
        }

        var filteredList = filtered.ToList();
        var total = filteredList.Count;
        var skip = (page - 1) * pageSize;
        var items = skip >= total ? new List<BibDupePair>() : filteredList.Skip(skip).Take(pageSize).ToList();

        var tomOptions = _pairs
            .Where(p => string.IsNullOrWhiteSpace(matchType) || p.Matches.Any(m => string.Equals(m.MatchType, matchType, StringComparison.OrdinalIgnoreCase)))
            .Where(p => !requiresHold || p.LeftHoldCount > 0 || p.RightHoldCount > 0)
            .GroupBy(p => new { p.PrimaryMarcTomId, p.TOM })
            .Where(g => g.Key.PrimaryMarcTomId != 0 && !string.IsNullOrWhiteSpace(g.Key.TOM))
            .OrderBy(g => g.Key.TOM, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TomOption(g.Key.PrimaryMarcTomId, g.Key.TOM!))
            .ToList();

        var matchTypeOptions = _pairs
            .Where(p => !tomId.HasValue || p.PrimaryMarcTomId == tomId.Value)
            .Where(p => !requiresHold || p.LeftHoldCount > 0 || p.RightHoldCount > 0)
            .SelectMany(p => p.Matches.Select(m => m.MatchType))
            .Where(mt => !string.IsNullOrWhiteSpace(mt))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mt => mt, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new PairsPageResult
        {
            Items = items,
            TotalCount = total,
            TomOptions = tomOptions,
            MatchTypeOptions = matchTypeOptions
        });
    }

    public Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId)
        => Task.FromResult(_pairs.FirstOrDefault(p => p.LeftBibId == leftBibId && p.RightBibId == rightBibId));

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) => Task.CompletedTask;

    public Task MarkNotDuplicateAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
