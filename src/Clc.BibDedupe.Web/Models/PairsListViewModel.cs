using System;
using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class PairsListViewModel
{
    public IEnumerable<BibDupePair> Items { get; set; } = new List<BibDupePair>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IEnumerable<TomOption> TomOptions { get; set; } = new List<TomOption>();
    public IEnumerable<string> MatchTypeOptions { get; set; } = new List<string>();
    public int? SelectedTomId { get; set; }
    public string? SelectedMatchType { get; set; }
    public bool? SelectedHasHolds { get; set; }
}
