using Clc.BibDedupe.Web.Services;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class InMemoryDecisionBatchTrackerTests
{
    [TestMethod]
    public async Task StartAsync_Allows_Only_One_Concurrent_Active_Batch_Per_User()
    {
        var tracker = new InMemoryDecisionBatchTracker();
        const string userEmail = "user@example.com";
        var startedAt = DateTimeOffset.UtcNow;

        var first = Task.Run(() => tracker.StartAsync(userEmail, startedAt));
        var second = Task.Run(() => tracker.StartAsync(userEmail, startedAt.AddMilliseconds(1)));

        await Task.WhenAll(
            first.ContinueWith(_ => { }),
            second.ContinueWith(_ => { }));

        var faultCount = new[] { first, second }.Count(t => t.IsFaulted);
        var successCount = new[] { first, second }.Count(t => t.Status == TaskStatus.RanToCompletion);

        successCount.Should().Be(1);
        faultCount.Should().Be(1);
        new[] { first, second }
            .Single(t => t.IsFaulted)
            .Exception!
            .GetBaseException()
            .Should()
            .BeOfType<ActiveDecisionBatchExistsException>();
    }
}
