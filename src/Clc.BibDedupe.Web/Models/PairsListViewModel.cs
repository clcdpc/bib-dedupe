using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Models;

public class PairsListViewModel
{
    public IEnumerable<BibDupePair> Items { get; set; } = new List<BibDupePair>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
