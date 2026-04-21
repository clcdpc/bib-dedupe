using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public sealed class ReviewPageResult
{
    public required IndexViewModel Model { get; init; }
    public required int LeftBibId { get; init; }
    public required int RightBibId { get; init; }
}
