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

        decision.LeftTitle = "Updated";
        decision.Matches[0].MatchType = "Changed";

        var stored = (await store.GetAllAsync(UserId)).Single();
        stored.LeftTitle.Should().Be("Left Title");
        stored.Matches.Should().NotBeSameAs(decision.Matches);
        stored.Matches.Single().MatchType.Should().Be("Title");
    }

    [TestMethod]
    public async Task Adding_A_Decision_For_The_Same_Pair_Replaces_The_Existing_Entry()
    {
        var store = CreateStore(out _);
        await store.AddAsync(UserId, CreateDecision(1, 2, BibDupePairAction.KeepLeft));

        var updated = CreateDecision(1, 2, BibDupePairAction.KeepRight);
        updated.LeftTitle = "New Left";
        updated.Matches.Add(new PairMatch { MatchType = "ISBN", MatchValue = "123" });

        await store.AddAsync(UserId, updated);

        var stored = (await store.GetAllAsync(UserId)).Single();
        stored.Action.Should().Be(BibDupePairAction.KeepRight);
        stored.LeftTitle.Should().Be("New Left");
        stored.Matches.Should().HaveCount(2);
        stored.Matches.Last().MatchType.Should().Be("ISBN");
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
        stored[0].LeftBibId.Should().Be(1);
        stored[0].RightBibId.Should().Be(2);
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

    private static SessionDecisionStore CreateStore(out TestSession session)
    {
        session = new TestSession();
        return new SessionDecisionStore(TestHttpContextAccessor.WithSession(session));
    }

    private static DecisionItem CreateDecision(int leftBibId, int rightBibId, BibDupePairAction action) => new()
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
        },
        Action = action
    };
}
