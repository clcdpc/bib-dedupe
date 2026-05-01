using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Options;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Tests.TestUtilities;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class PairAssignmentCleanupJobTests
{
    [TestMethod]
    public async Task Skipping_Cleanup_When_The_Minimum_Age_Is_Not_Positive()
    {
        var storeMock = new Mock<IPairAssignmentStore>(MockBehavior.Strict);
        var options = Microsoft.Extensions.Options.Options.Create(new PairAssignmentCleanupOptions { MinimumAssignmentAge = TimeSpan.Zero });
        var logger = new TestLogger<PairAssignmentCleanupJob>();

        var job = new PairAssignmentCleanupJob(storeMock.Object, options, logger);

        await job.CleanupAsync();

        storeMock.VerifyNoOtherCalls();
        logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information)
            .Which.Message.Should().Contain("Skipping pair assignment cleanup");
    }

    [TestMethod]
    public async Task Releasing_Expired_Assignments_When_The_Minimum_Age_Is_Positive()
    {
        var storeMock = new Mock<IPairAssignmentStore>();
        DateTimeOffset? capturedCutoff = null;
        storeMock
            .Setup(s => s.ReleaseExpiredAsync(It.IsAny<DateTimeOffset>()))
            .Callback<DateTimeOffset>(cutoff => capturedCutoff = cutoff)
            .ReturnsAsync(3);
        var options = Microsoft.Extensions.Options.Options.Create(new PairAssignmentCleanupOptions { MinimumAssignmentAge = TimeSpan.FromHours(2) });
        var logger = new TestLogger<PairAssignmentCleanupJob>();

        var job = new PairAssignmentCleanupJob(storeMock.Object, options, logger);

        await job.CleanupAsync();

        storeMock.Verify(s => s.ReleaseExpiredAsync(It.IsAny<DateTimeOffset>()), Times.Once);
        capturedCutoff.Should().NotBeNull();
        (DateTimeOffset.Now - capturedCutoff!.Value).Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromSeconds(5));

        logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information)
            .Which.Message.Should().Contain("Released 3 pair assignments");
    }

    [TestMethod]
    public async Task Logging_Debug_When_No_Assignments_Are_Released()
    {
        var storeMock = new Mock<IPairAssignmentStore>();
        storeMock.Setup(s => s.ReleaseExpiredAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(0);
        var options = Microsoft.Extensions.Options.Options.Create(new PairAssignmentCleanupOptions { MinimumAssignmentAge = TimeSpan.FromMinutes(5) });
        var logger = new TestLogger<PairAssignmentCleanupJob>();

        var job = new PairAssignmentCleanupJob(storeMock.Object, options, logger);

        await job.CleanupAsync();

        logger.Entries.Should().ContainSingle(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Debug)
            .Which.Message.Should().Contain("No pair assignments older than");
    }
}
