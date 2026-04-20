namespace Clc.BibDedupe.Web.Services;

public sealed class PostDecisionNavigationResult
{
    public string? NextPairUrl { get; init; }
    public bool HasNextPair { get; init; }
    public bool ReReview { get; init; }
}
