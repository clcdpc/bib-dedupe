namespace Clc.BibDedupe.Web.Services;

public interface IPostDecisionNavigationService
{
    Task<PostDecisionNavigationResult> GetNavigationAsync(
        string userEmail,
        bool isReReview,
        (int leftBibId, int rightBibId) resolvedPair,
        Func<int, int, string?> pairUrlFactory,
        Func<string?> emptyReviewUrlFactory,
        Func<string?> decisionsIndexUrlFactory,
        Func<string?> fallbackUrlFactory);
}
