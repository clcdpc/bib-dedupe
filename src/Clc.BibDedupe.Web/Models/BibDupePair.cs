namespace Clc.BibDedupe.Web.Models
{
    public class BibDupePair
    {
        public int PairId { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public string MatchValue { get; set; } = string.Empty;
        public int PrimaryMarcTomId { get; set; }
        public int LeftBibId { get; set; }
        public int RightBibId { get; set; }
        public string LeftTitle { get; set; } = string.Empty;
        public string LeftAuthor { get; set; } = string.Empty;
        public string RightTitle { get; set; } = string.Empty;
        public string RightAuthor { get; set; } = string.Empty;
    }
}
