namespace Clc.BibDedupe.Web.Models
{
    public class IndexViewModel
    {
        public int LeftBibId { get; set; }
        public int RightBibId { get; set; }
        public string LeftBibXml { get; set; } = string.Empty;
        public string RightBibXml { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }
}
