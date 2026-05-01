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

    [TestMethod]
    public async Task SetJobIdAsync_Allows_Attach_After_Batch_Becomes_Terminal()
    {
        var tracker = new InMemoryDecisionBatchTracker();
        const string userEmail = "user@example.com";
        var startedAt = DateTimeOffset.UtcNow;

        var started = await tracker.StartAsync(userEmail, startedAt);
        await tracker.CompleteAsync(userEmail, startedAt.AddSeconds(1));

        var updated = await tracker.SetJobIdAsync(started.BatchId, "job-42");

        updated.JobId.Should().Be("job-42");
        updated.IsCompleted.Should().BeTrue();
    }
}
