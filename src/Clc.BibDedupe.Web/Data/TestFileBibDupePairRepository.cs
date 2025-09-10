using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data;

public class TestFileBibDupePairRepository : IBibDupePairRepository
{
    private int _nextId;
    private readonly int _pairCount;

    public TestFileBibDupePairRepository(int pairCount = 10)
    {
        _pairCount = pairCount;
    }

    private BibDupePair CreatePair() => new()
    {
        MatchType = "Test",
        MatchValue = "Test",
        LeftBibId = Interlocked.Increment(ref _nextId),
        RightBibId = Interlocked.Increment(ref _nextId)
    };

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

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail) => Task.CompletedTask;

    public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
