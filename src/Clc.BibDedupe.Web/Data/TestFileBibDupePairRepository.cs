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
        return new BibDupePair
        {
            PairId = pairId,
            LeftBibId = left,
            RightBibId = right,
            PrimaryMarcTomId = left,
            LeftTitle = $"Left Title {pairId}",
            LeftAuthor = $"Left Author {pairId}",
            RightTitle = $"Right Title {pairId}",
            RightAuthor = $"Right Author {pairId}",
            LeftHoldCount = 0,
            RightHoldCount = 0,
            TotalHoldCount = 0,
            Matches = new List<PairMatch>
            {
                new()
                {
                    MatchType = "Test",
                    MatchValue = "Test"
                }
            }
        };
    }

    private IEnumerable<BibDupePair> GeneratePairs(int count)
        => Enumerable.Range(0, count).Select(_ => CreatePair());

    public Task<IEnumerable<BibDupePair>> GetAsync()
        => Task.FromResult<IEnumerable<BibDupePair>>(_pairs);

    public Task<(IEnumerable<BibDupePair> Items, int TotalCount, int TotalPages)> GetPagedAsync(int page, int pageSize)
    {
        var total = _pairs.Count;
        if (total == 0)
        {
            return Task.FromResult(((IEnumerable<BibDupePair>)Enumerable.Empty<BibDupePair>(), 0, 0));
        }

        var normalizedPageSize = Math.Max(pageSize, 1);
        var normalizedPage = Math.Max(page, 1);

        var grouped = _pairs
            .GroupBy(p => p.PrimaryMarcTomId)
            .OrderBy(g => g.Key)
            .ToList();

        var groupPages = new Dictionary<int, int>();
        var runningTotal = 0;
        foreach (var group in grouped)
        {
            var pageNumber = runningTotal / normalizedPageSize + 1;
            groupPages[group.Key] = pageNumber;
            runningTotal += group.Count();
        }

        var totalPages = groupPages.Count == 0
            ? 0
            : groupPages.Values.DefaultIfEmpty(0).Max();

        var items = grouped
            .Where(g => groupPages[g.Key] == normalizedPage)
            .SelectMany(g => g.OrderBy(p => p.PairId))
            .ToList();

        return Task.FromResult(((IEnumerable<BibDupePair>)items, total, totalPages));
    }

    public Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId)
        => Task.FromResult(_pairs.FirstOrDefault(p => p.LeftBibId == leftBibId && p.RightBibId == rightBibId));

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) => Task.CompletedTask;

    public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
