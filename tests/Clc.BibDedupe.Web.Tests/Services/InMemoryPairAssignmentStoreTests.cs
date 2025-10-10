using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Services;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class InMemoryPairAssignmentStoreTests
{
    [TestMethod]
    public async Task Releasing_With_A_Matching_User_Removes_The_Assignment()
    {
        var store = new InMemoryPairAssignmentStore();
        await store.AssignAsync("user", 1, 2);

        await store.ReleaseAsync("user", 1, 2);

        var removed = await store.ReleaseExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(1));
        removed.Should().Be(0);
    }

    [TestMethod]
    public async Task Releasing_With_A_Different_User_Does_Not_Remove_The_Assignment()
    {
        var store = new InMemoryPairAssignmentStore();
        await store.AssignAsync("user", 1, 2);

        await store.ReleaseAsync("other", 1, 2);

        var removed = await store.ReleaseExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(1));
        removed.Should().Be(1);
    }

    [TestMethod]
    public async Task Releasing_Expired_Assignments_Only_Removes_Entries_Older_Than_The_Cutoff()
    {
        var store = new InMemoryPairAssignmentStore();
        await store.AssignAsync("user", 1, 2);

        var noneRemoved = await store.ReleaseExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        noneRemoved.Should().Be(0);

        await store.AssignAsync("user", 3, 4);

        var removed = await store.ReleaseExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(1));
        removed.Should().Be(2);
    }
}
