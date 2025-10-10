using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Models
{
    public class BibDupePair
    {
        public int PairId { get; set; }
        public int PrimaryMarcTomId { get; set; }
        public int LeftBibId { get; set; }
        public int RightBibId { get; set; }
        public string? LeftTitle { get; set; }
        public string? LeftAuthor { get; set; }
        public string? RightTitle { get; set; }
        public string? RightAuthor { get; set; }
        public string? TOM { get; set; }
        public int LeftHoldCount { get; set; }
        public int RightHoldCount { get; set; }
        public int TotalHoldCount { get; set; }
        public List<PairMatch> Matches { get; set; } = new();

        public string MatchSummary => Matches.Count == 0
            ? string.Empty
            : string.Join(", ", Matches.Select(m => $"{m.MatchType}: {m.MatchValue}"));

        public BibDupePair Clone() => new()
        {
            PairId = PairId,
            PrimaryMarcTomId = PrimaryMarcTomId,
            LeftBibId = LeftBibId,
            RightBibId = RightBibId,
            LeftTitle = LeftTitle,
            LeftAuthor = LeftAuthor,
            RightTitle = RightTitle,
            RightAuthor = RightAuthor,
            TOM = TOM,
            LeftHoldCount = LeftHoldCount,
            RightHoldCount = RightHoldCount,
            TotalHoldCount = TotalHoldCount,
            Matches = PairMatch.CloneList(Matches)
        };
    }
}
