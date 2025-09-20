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
        public List<PairMatch> Matches { get; set; } = new();

        public string MatchSummary => Matches.Count == 0
            ? string.Empty
            : string.Join(", ", Matches.Select(m => $"{m.MatchType}: {m.MatchValue}"));
    }
}
