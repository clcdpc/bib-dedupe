namespace Clc.BibDedupe.Web.Models
{
    public class BibDupePair
    {
        public string MatchType { get; set; } = string.Empty;
        public string MatchValue { get; set; } = string.Empty;
        public int LeftBibId { get; set; }
        public int RightBibId { get; set; }
    }
}
