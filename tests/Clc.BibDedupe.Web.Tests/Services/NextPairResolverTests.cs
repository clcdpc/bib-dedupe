using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class NextPairResolverTests
{
    private const string UserEmail = "user@example.com";

    [TestMethod]
    public async Task Getting_Next_Pair_Without_Explicit_Filters_Uses_Stored_Filters_And_Returns_First_Pair()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var filterStoreMock = new Mock<IPairFilterStore>(MockBehavior.Strict);
        var filters = new PairFilterOptions { TomId = 7, MatchType = "Isbn", HasHolds = true };

        filterStoreMock.Setup(s => s.GetAsync(UserEmail)).ReturnsAsync(filters);
        repositoryMock
            .Setup(r => r.GetAsync(UserEmail, 7, "Isbn", true, true))
            .ReturnsAsync(new[]
            {
                new BibDupePair { LeftBibId = 1, RightBibId = 2 },
                new BibDupePair { LeftBibId = 3, RightBibId = 4 }
            });

        var resolver = new NextPairResolver(repositoryMock.Object, filterStoreMock.Object);

        var result = await resolver.GetNextPairForUserAsync(UserEmail, filters: null);

        result.Should().NotBeNull();
        result!.LeftBibId.Should().Be(1);
        result.RightBibId.Should().Be(2);

        repositoryMock.VerifyAll();
        filterStoreMock.VerifyAll();
    }

    [TestMethod]
    public async Task Getting_Next_Pair_With_ExcludePair_Skips_Current_Pair()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var filterStoreMock = new Mock<IPairFilterStore>(MockBehavior.Strict);
        var explicitFilters = new PairFilterOptions { MatchType = "Oclc" };

        repositoryMock
            .Setup(r => r.GetAsync(UserEmail, null, "Oclc", null, true))
            .ReturnsAsync(new[]
            {
                new BibDupePair { LeftBibId = 10, RightBibId = 20 },
                new BibDupePair { LeftBibId = 11, RightBibId = 21 }
            });

        var resolver = new NextPairResolver(repositoryMock.Object, filterStoreMock.Object);

        var result = await resolver.GetNextPairForUserAsync(UserEmail, explicitFilters, excludePair: (10, 20));

        result.Should().NotBeNull();
        result!.LeftBibId.Should().Be(11);
        result.RightBibId.Should().Be(21);

        repositoryMock.VerifyAll();
        filterStoreMock.VerifyNoOtherCalls();
    }
}
