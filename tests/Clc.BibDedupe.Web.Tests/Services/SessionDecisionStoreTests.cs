using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Tests.TestUtilities;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class SessionDecisionStoreTests
{
    private const string UserId = "user";

    [TestMethod]
    public async Task Adding_A_Decision_Stores_A_Clone_Of_The_Item()
    {
        var store = CreateStore(out _);
        var decision = CreateDecision(1, 2, BibDupePairAction.KeepLeft);

        await store.AddAsync(UserId, decision);

        decision.Pair.LeftTitle = "Updated";
        decision.Pair.Matches[0].MatchType = "Changed";

        var stored = (await store.GetAllAsync(UserId)).Single();
        stored.Pair.LeftTitle.Should().Be("Left Title");
        stored.Pair.Matches.Should().NotBeSameAs(decision.Pair.Matches);
        stored.Pair.Matches.Single().MatchType.Should().Be("Title");
    }

    [TestMethod]
    public async Task Adding_A_Decision_For_The_Same_Pair_Replaces_The_Existing_Entry()
    {
        var store = CreateStore(out _);
        await store.AddAsync(UserId, CreateDecision(1, 2, BibDupePairAction.KeepLeft));

        var updated = CreateDecision(1, 2, BibDupePairAction.KeepRight);
        updated.Pair.LeftTitle = "New Left";
        updated.Pair.Matches.Add(new PairMatch { MatchType = "ISBN", MatchValue = "123" });

        await store.AddAsync(UserId, updated);

        var stored = (await store.GetAllAsync(UserId)).Single();
        stored.Action.Should().Be(BibDupePairAction.KeepRight);
        stored.Pair.LeftTitle.Should().Be("New Left");
        stored.Pair.Matches.Should().HaveCount(2);
        stored.Pair.Matches.Last().MatchType.Should().Be("ISBN");
    }

    [TestMethod]
    public async Task Adding_A_Conflicting_Decision_Throws_A_Conflict_Exception()
    {
        var store = CreateStore(out _);
        await store.AddAsync(UserId, CreateDecision(1, 2, BibDupePairAction.KeepLeft));

        var conflicting = CreateDecision(2, 3, BibDupePairAction.KeepLeft);

        await Assert.ThrowsExceptionAsync<DecisionConflictException>(() => store.AddAsync(UserId, conflicting));

        var stored = (await store.GetAllAsync(UserId)).ToList();
        stored.Should().HaveCount(1);
        stored[0].Pair.LeftBibId.Should().Be(1);
        stored[0].Pair.RightBibId.Should().Be(2);
    }

    [TestMethod]
    public async Task Removing_A_Decision_Removes_It_From_The_Store()
    {
        var store = CreateStore(out _);
        await store.AddAsync(UserId, CreateDecision(1, 2, BibDupePairAction.KeepLeft));

        await store.RemoveAsync(UserId, 1, 2);

        (await store.CountAsync(UserId)).Should().Be(0);
        (await store.GetAllAsync(UserId)).Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAsync_Returns_A_Clone_When_A_Decision_Exists()
    {
        var store = CreateStore(out _);
        var decision = CreateDecision(1, 2, BibDupePairAction.KeepLeft);

        await store.AddAsync(UserId, decision);

        var stored = await store.GetAsync(UserId, 1, 2);

        stored.Should().NotBeNull();
        stored!.Should().NotBeSameAs(decision);
        stored.Pair.Matches.Should().NotBeSameAs(decision.Pair.Matches);
        stored.Action.Should().Be(BibDupePairAction.KeepLeft);
    }

    [TestMethod]
    public async Task GetAsync_Returns_Null_When_No_Decision_Exists()
    {
        var store = CreateStore(out _);

        var stored = await store.GetAsync(UserId, 1, 2);

        stored.Should().BeNull();
    }

    private static SessionDecisionStore CreateStore(out TestSession session)
    {
        session = new TestSession();
        return new SessionDecisionStore(TestHttpContextAccessor.WithSession(session));
    }

    private static DecisionItem CreateDecision(int leftBibId, int rightBibId, BibDupePairAction action) => new()
    {
        Pair = new BibDupePair
        {
            LeftBibId = leftBibId,
            RightBibId = rightBibId,
            LeftTitle = "Left Title",
            LeftAuthor = "Left Author",
            RightTitle = "Right Title",
            RightAuthor = "Right Author",
            TOM = "TOM",
            PrimaryMarcTomId = 42,
            Matches = new List<PairMatch>
            {
                new()
                {
                    MatchType = "Title",
                    MatchValue = "Match"
                }
            }
        },
        Action = action
    };
}
