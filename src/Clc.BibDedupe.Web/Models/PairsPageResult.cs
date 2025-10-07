using System;
using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class PairsPageResult
{
    public IEnumerable<BibDupePair> Items { get; init; } = new List<BibDupePair>();
    public int TotalCount { get; init; }
    public IReadOnlyCollection<TomOption> TomOptions { get; init; } = Array.Empty<TomOption>();
    public IReadOnlyCollection<string> MatchTypeOptions { get; init; } = Array.Empty<string>();
}
