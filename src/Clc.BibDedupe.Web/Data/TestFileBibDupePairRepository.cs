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

    public Task<IEnumerable<BibDupePair>> GetAsync(
        string? userEmail = null,
        int? tomId = null,
        string? matchType = null,
        bool? hasHolds = null,
        bool hideDecided = true)
    {
        var filtered = ApplyFilters(_pairs, tomId, matchType, hasHolds);
        return Task.FromResult<IEnumerable<BibDupePair>>(filtered.ToList());
    }

    private static IEnumerable<BibDupePair> ApplyFilters(
        IEnumerable<BibDupePair> source,
        int? tomId,
        string? matchType,
        bool? hasHolds)
    {
        var result = source;

        if (tomId.HasValue)
        {
            result = result.Where(p => p.PrimaryMarcTomId == tomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(matchType))
        {
            result = result.Where(p => p.Matches.Any(m => string.Equals(m.MatchType, matchType, StringComparison.OrdinalIgnoreCase)));
        }

        result = result.Where(p => hasHolds switch
        {
            true => p.LeftHoldCount > 0 || p.RightHoldCount > 0,
            false => p.LeftHoldCount == 0 && p.RightHoldCount == 0,
            _ => true
        });

        return result;
    }

    public Task<PairsPageResult> GetPagedAsync(
        int page,
        int pageSize,
        string? userEmail = null,
        int? tomId = null,
        string? matchType = null,
        bool? hasHolds = null,
        bool hideDecided = true)
    {
        var filteredList = ApplyFilters(_pairs, tomId, matchType, hasHolds).ToList();
        var total = filteredList.Count;
        var skip = (page - 1) * pageSize;
        var items = skip >= total ? new List<BibDupePair>() : filteredList.Skip(skip).Take(pageSize).ToList();

        var tomOptions = ApplyFilters(_pairs, null, matchType, hasHolds)
            .GroupBy(p => new { p.PrimaryMarcTomId, p.TOM })
            .Where(g => g.Key.PrimaryMarcTomId != 0 && !string.IsNullOrWhiteSpace(g.Key.TOM))
            .OrderBy(g => g.Key.TOM, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TomOption(g.Key.PrimaryMarcTomId, g.Key.TOM!))
            .ToList();

        var matchTypeOptions = ApplyFilters(_pairs, tomId, null, hasHolds)
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

    public Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId, string? userEmail = null, bool hideDecided = true)
        => Task.FromResult(_pairs.FirstOrDefault(p => p.LeftBibId == leftBibId && p.RightBibId == rightBibId));

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) => Task.CompletedTask;

    public Task MarkNotDuplicateAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
