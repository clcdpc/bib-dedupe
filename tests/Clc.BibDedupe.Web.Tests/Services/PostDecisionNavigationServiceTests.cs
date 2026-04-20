using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class PostDecisionNavigationServiceTests
{
    private const string UserEmail = "user@example.com";

    [TestMethod]
    public async Task Normal_Review_Uses_Next_Pair_Resolver_With_Exclude_And_Returns_Next_Pair_Url()
    {
        var nextPairResolverMock = new Mock<INextPairResolver>(MockBehavior.Strict);
        var pairFilterStoreMock = new Mock<IPairFilterStore>(MockBehavior.Strict);
        var filters = new PairFilterOptions { TomId = 3, MatchType = "Oclc" };

        pairFilterStoreMock.Setup(s => s.GetAsync(UserEmail)).ReturnsAsync(filters);
        nextPairResolverMock
            .Setup(r => r.GetNextPairForUserAsync(UserEmail, filters, (10, 20)))
            .ReturnsAsync(new BibDupePair { LeftBibId = 30, RightBibId = 40 });

        var service = new PostDecisionNavigationService(nextPairResolverMock.Object, pairFilterStoreMock.Object);

        var result = await service.GetNavigationAsync(
            UserEmail,
            isReReview: false,
            resolvedPair: (10, 20),
            pairUrlFactory: (left, right) => $"/review/{left}/{right}",
            emptyReviewUrlFactory: () => "/review",
            decisionsIndexUrlFactory: () => "/decisions",
            fallbackUrlFactory: () => "/pairs");

        result.ReReview.Should().BeFalse();
        result.HasNextPair.Should().BeTrue();
        result.NextPairUrl.Should().Be("/review/30/40");

        pairFilterStoreMock.VerifyAll();
        nextPairResolverMock.VerifyAll();
    }

    [TestMethod]
    public async Task ReReview_Navigates_To_Decisions_Index_Without_Resolving_Next_Pair()
    {
        var nextPairResolverMock = new Mock<INextPairResolver>(MockBehavior.Strict);
        var pairFilterStoreMock = new Mock<IPairFilterStore>(MockBehavior.Strict);
        var service = new PostDecisionNavigationService(nextPairResolverMock.Object, pairFilterStoreMock.Object);

        var result = await service.GetNavigationAsync(
            UserEmail,
            isReReview: true,
            resolvedPair: (10, 20),
            pairUrlFactory: (left, right) => $"/review/{left}/{right}",
            emptyReviewUrlFactory: () => "/review",
            decisionsIndexUrlFactory: () => "/decisions",
            fallbackUrlFactory: () => "/pairs");

        result.ReReview.Should().BeTrue();
        result.HasNextPair.Should().BeFalse();
        result.NextPairUrl.Should().Be("/decisions");

        pairFilterStoreMock.VerifyNoOtherCalls();
        nextPairResolverMock.VerifyNoOtherCalls();
    }
}
