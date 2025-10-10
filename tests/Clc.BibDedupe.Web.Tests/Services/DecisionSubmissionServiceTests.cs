using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class DecisionSubmissionServiceTests
{
    private const string UserEmail = "user@example.com";

    [TestMethod]
    public async Task Submitting_When_A_Batch_Is_Already_In_Progress_Returns_That_Status()
    {
        var storeMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>(MockBehavior.Strict);
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<DecisionSubmissionService>>();

        var status = new DecisionBatchStatus { JobId = "existing", StartedAt = DateTimeOffset.UtcNow };
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync(status);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            loggerMock.Object);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("A batch is already being processed.");
        result.BatchStatus.Should().BeSameAs(status);

        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        storeMock.VerifyNoOtherCalls();
        executorMock.VerifyNoOtherCalls();
        backgroundJobsMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submitting_When_Processing_Is_Unavailable_Returns_An_Error()
    {
        var storeMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<DecisionSubmissionService>>();

        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(false);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            loggerMock.Object);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        result.BatchStatus.Should().BeNull();

        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        executorMock.Verify(e => e.CanProcessAsync(), Times.Once);
        storeMock.VerifyNoOtherCalls();
        backgroundJobsMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submitting_When_No_Decisions_Exist_Returns_An_Error()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<DecisionSubmissionService>>();

        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(0);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            loggerMock.Object);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("There are no decisions to submit.");
        result.BatchStatus.Should().BeNull();

        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        executorMock.Verify(e => e.CanProcessAsync(), Times.Once);
        storeMock.Verify(s => s.CountAsync(UserEmail), Times.Once);
        trackerMock.VerifyNoOtherCalls();
        backgroundJobsMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submitting_When_Ready_Starts_Background_Processing()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var loggerMock = new Mock<ILogger<DecisionSubmissionService>>();

        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(3);

        Job? capturedJob = null;
        backgroundJobsMock
            .Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => capturedJob = job)
            .Returns("job-123");

        trackerMock
            .Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>(), "job-123"))
            .ReturnsAsync((string _, DateTimeOffset startedAt, string jobId) => new DecisionBatchStatus
            {
                JobId = jobId,
                StartedAt = startedAt
            });

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            loggerMock.Object);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.BatchStatus.Should().NotBeNull();
        result.BatchStatus!.JobId.Should().Be("job-123");
        result.BatchStatus.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        capturedJob.Should().NotBeNull();
        capturedJob!.Type.Should().Be(typeof(DecisionProcessingJob));
        capturedJob.Method.Name.Should().Be(nameof(DecisionProcessingJob.ExecuteAsync));
        capturedJob.Args.Should().HaveCount(1);
        capturedJob.Args[0].Should().Be(UserEmail);

        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        executorMock.Verify(e => e.CanProcessAsync(), Times.Once);
        storeMock.Verify(s => s.CountAsync(UserEmail), Times.Once);
        trackerMock.Verify(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>(), "job-123"), Times.Once);
        backgroundJobsMock.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }
}
