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
    private readonly int _pairCount;

    public TestFileBibDupePairRepository(int pairCount = 10)
    {
        _pairCount = pairCount;
    }

    private BibDupePair CreatePair()
    {
        var pairId = Interlocked.Increment(ref _nextPairId);
        var left = Interlocked.Increment(ref _nextBibId);
        var right = Interlocked.Increment(ref _nextBibId);
        return new BibDupePair
        {
            PairId = pairId,
            MatchType = "Test",
            MatchValue = "Test",
            LeftBibId = left,
            RightBibId = right,
            PrimaryMarcTomId = left
        };
    }

    private IEnumerable<BibDupePair> GeneratePairs(int count)
        => Enumerable.Range(0, count).Select(_ => CreatePair());

    public Task<IEnumerable<BibDupePair>> GetAsync()
        => Task.FromResult(GeneratePairs(_pairCount));

    public Task<(IEnumerable<BibDupePair> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var total = _pairCount;
        var skip = (page - 1) * pageSize;
        if (skip >= total)
        {
            return Task.FromResult(((IEnumerable<BibDupePair>)Enumerable.Empty<BibDupePair>(), total));
        }

        var take = Math.Min(pageSize, total - skip);
        var items = GeneratePairs(take);
        return Task.FromResult((items, total));
    }

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) => Task.CompletedTask;

    public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
