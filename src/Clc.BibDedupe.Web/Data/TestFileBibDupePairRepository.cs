using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data;

public class TestFileBibDupePairRepository : IBibDupePairRepository
{
    private int _nextId;

    private BibDupePair CreatePair() => new()
    {
        MatchType = "Test",
        MatchValue = "Test",
        LeftBibId = Interlocked.Increment(ref _nextId),
        RightBibId = Interlocked.Increment(ref _nextId)
    };

    public Task<IEnumerable<BibDupePair>> GetAsync()
        => Task.FromResult<IEnumerable<BibDupePair>>(new[] { CreatePair() });

    public Task<(IEnumerable<BibDupePair> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        => Task.FromResult(((IEnumerable<BibDupePair>)new[] { CreatePair() }, 1));

    public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail) => Task.CompletedTask;

    public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;

    public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) => Task.CompletedTask;
}
