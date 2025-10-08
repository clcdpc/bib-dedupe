using System;

namespace Clc.BibDedupe.Web.Models;

public class PairFilterOptions
{
    public int? TomId { get; set; }
    public string? MatchType { get; set; }
    public bool? HasHolds { get; set; }

    public bool IsEmpty => TomId is null && string.IsNullOrWhiteSpace(MatchType) && HasHolds is null;

    public PairFilterOptions Normalize()
    {
        MatchType = string.IsNullOrWhiteSpace(MatchType) ? null : MatchType.Trim();
        return this;
    }
}
