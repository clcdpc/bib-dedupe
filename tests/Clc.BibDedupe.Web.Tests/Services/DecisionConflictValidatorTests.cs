using System;
using System.Collections.Generic;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class DecisionConflictValidatorTests
{
    [TestMethod]
    public void Allowing_Distinct_Merges_Does_Not_Throw()
    {
        var items = new List<DecisionItem>
        {
            CreateDecision(1, 2, BibDupePairAction.KeepLeft),
            CreateDecision(3, 4, BibDupePairAction.KeepRight),
            CreateDecision(5, 6, BibDupePairAction.Skip)
        };

        Action act = () => DecisionConflictValidator.EnsureNoMergeConflicts(items);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Merging_The_Same_Bib_Twice_Throws_A_Conflict_Exception()
    {
        var items = new List<DecisionItem>
        {
            CreateDecision(1, 2, BibDupePairAction.KeepLeft),
            CreateDecision(3, 2, BibDupePairAction.KeepLeft)
        };

        Action act = () => DecisionConflictValidator.EnsureNoMergeConflicts(items);

        act.Should().Throw<DecisionConflictException>()
            .Which.Message.Should().Contain("Bib 2 has already been selected to merge into another record");
    }

    [TestMethod]
    public void Keeping_A_Bib_That_Was_Merged_Elsewhere_Throws()
    {
        var items = new List<DecisionItem>
        {
            CreateDecision(1, 2, BibDupePairAction.KeepLeft),
            CreateDecision(1, 3, BibDupePairAction.KeepRight)
        };

        Action act = () => DecisionConflictValidator.EnsureNoMergeConflicts(items);

        act.Should().Throw<DecisionConflictException>()
            .Which.Message.Should().Contain("Bib 1 is already the kept record");
    }

    [TestMethod]
    public void Merging_A_Bib_That_Was_Already_Kept_Throws()
    {
        var items = new List<DecisionItem>
        {
            CreateDecision(1, 2, BibDupePairAction.KeepLeft),
            CreateDecision(2, 3, BibDupePairAction.KeepLeft)
        };

        Action act = () => DecisionConflictValidator.EnsureNoMergeConflicts(items);

        act.Should().Throw<DecisionConflictException>()
            .Which.Message.Should().Contain("Bib 2 has already been merged into a different record");
    }

    private static DecisionItem CreateDecision(int leftBibId, int rightBibId, BibDupePairAction action) => new()
    {
        Pair = new BibDupePair
        {
            LeftBibId = leftBibId,
            RightBibId = rightBibId
        },
        Action = action
    };
}
