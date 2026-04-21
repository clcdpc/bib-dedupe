using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class PostDecisionNavigationService(INextPairResolver nextPairResolver, IPairFilterStore pairFilterStore) : IPostDecisionNavigationService
{
    public async Task<PostDecisionNavigationResult> GetNavigationAsync(
        string userEmail,
        bool isReReview,
        (int leftBibId, int rightBibId) resolvedPair,
        Func<int, int, string?> pairUrlFactory,
        Func<string?> emptyReviewUrlFactory,
        Func<string?> decisionsIndexUrlFactory,
        Func<string?> fallbackUrlFactory)
    {
        if (isReReview)
        {
            return new PostDecisionNavigationResult
            {
                NextPairUrl = decisionsIndexUrlFactory() ?? fallbackUrlFactory() ?? "/",
                HasNextPair = false,
                ReReview = true
            };
        }

        var filters = await pairFilterStore.GetAsync(userEmail);
        var nextPair = await nextPairResolver.GetNextPairForUserAsync(userEmail, filters, resolvedPair);

        var nextPairUrl = nextPair is not null
            ? pairUrlFactory(nextPair.LeftBibId, nextPair.RightBibId)
            : emptyReviewUrlFactory();

        return new PostDecisionNavigationResult
        {
            NextPairUrl = nextPairUrl ?? fallbackUrlFactory() ?? "/",
            HasNextPair = nextPair is not null,
            ReReview = false
        };
    }
}
