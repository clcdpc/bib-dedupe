using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;
using Clc.BibDedupe.Web.Tests.TestUtilities;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class DecisionSubmissionServiceTests
{
    private const string UserEmail = "user@example.com";

    [TestMethod]
    public async Task Getting_The_Current_Batch_Returns_Tracker_Result()
    {
        var storeMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var trackerMock = new Mock<IDecisionBatchTracker>(MockBehavior.Strict);
        var executorMock = new Mock<IDecisionProcessingExecutor>(MockBehavior.Strict);
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new TestLogger<DecisionSubmissionService>();

        var expected = new DecisionBatchStatus { JobId = "job-7", StartedAt = DateTimeOffset.UtcNow };
        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync(expected);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.GetCurrentBatchAsync(UserEmail);

        result.Should().BeSameAs(expected);

        trackerMock.Verify(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."), Times.Once);
        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        storeMock.VerifyNoOtherCalls();
        executorMock.VerifyNoOtherCalls();
        backgroundJobsMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submitting_When_A_Batch_Is_Already_In_Progress_Returns_That_Status()
    {
        var storeMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>(MockBehavior.Strict);
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        var status = new DecisionBatchStatus { JobId = "existing", StartedAt = DateTimeOffset.UtcNow };
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync(status);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

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
    public async Task Submitting_When_Processing_Is_Unavailable_Returns_An_Error_And_Logs_A_Warning()
    {
        var storeMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(false);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        result.BatchStatus.Should().BeNull();

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Decision processing is not available for user@example.com", StringComparison.Ordinal));

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
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(0);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

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
    public async Task Submitting_When_Ready_Starts_Background_Processing_And_Logs_Information()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var logger = new TestLogger<DecisionSubmissionService>();

        var sequence = new MockSequence();
        trackerMock.InSequence(sequence)
            .Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.InSequence(sequence)
            .Setup(t => t.GetCurrentAsync(UserEmail))
            .ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(3);

        Job? capturedJob = null;
        trackerMock.InSequence(sequence)
            .Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((string _, DateTimeOffset start) => new DecisionBatchStatus { BatchId = 42, JobId = string.Empty, StartedAt = start });
        backgroundJobsMock.InSequence(sequence)
            .Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => capturedJob = job)
            .Returns("job-123");
        trackerMock.InSequence(sequence)
            .Setup(t => t.SetJobIdAsync(42, "job-123"))
            .ReturnsAsync(new DecisionBatchStatus { BatchId = 42, JobId = "job-123", StartedAt = DateTimeOffset.UtcNow });

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.BatchStatus.Should().NotBeNull();
        result.BatchStatus!.JobId.Should().Be("job-123");
        result.BatchStatus.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Queued decision processing job job-123 for user@example.com", StringComparison.Ordinal));

        capturedJob.Should().NotBeNull();
        capturedJob!.Type.Should().Be(typeof(DecisionProcessingJob));
        capturedJob.Method.Name.Should().Be(nameof(DecisionProcessingJob.ExecuteAsync));
        capturedJob.Args.Should().HaveCount(1);
        capturedJob.Args[0].Should().Be(UserEmail);

        trackerMock.Verify(t => t.GetCurrentAsync(UserEmail), Times.Once);
        trackerMock.Verify(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."), Times.Once);
        executorMock.Verify(e => e.CanProcessAsync(), Times.Once);
        storeMock.Verify(s => s.CountAsync(UserEmail), Times.Once);
        trackerMock.Verify(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()), Times.Once);
        trackerMock.Verify(t => t.SetJobIdAsync(42, "job-123"), Times.Once);
        backgroundJobsMock.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [TestMethod]
    public async Task Submitting_When_Concurrent_Start_Detects_Existing_Active_Batch_Returns_Already_In_Progress()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        var active = new DecisionBatchStatus { JobId = "job-existing", StartedAt = DateTimeOffset.UtcNow };
        trackerMock.SetupSequence(t => t.GetCurrentAsync(UserEmail))
            .ReturnsAsync((DecisionBatchStatus?)null)
            .ReturnsAsync(active);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(2);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new ActiveDecisionBatchExistsException(UserEmail));

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("A batch is already being processed.");
        result.BatchStatus.Should().BeSameAs(active);
        backgroundJobsMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submitting_When_Concurrent_Start_Reports_Conflict_But_Current_Batch_Is_Missing_Returns_Processing_Unavailable()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.SetupSequence(t => t.GetCurrentAsync(UserEmail))
            .ReturnsAsync((DecisionBatchStatus?)null)
            .ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(1);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new ActiveDecisionBatchExistsException(UserEmail));

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        result.BatchStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task Submitting_When_Enqueue_Fails_Marks_Batch_Failed_And_Returns_Processing_Unavailable()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(1);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((string _, DateTimeOffset start) => new DecisionBatchStatus { BatchId = 50, JobId = string.Empty, StartedAt = start });
        backgroundJobsMock.Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Throws(new InvalidOperationException("hangfire offline"));
        trackerMock.Setup(t => t.FailAsync(UserEmail, It.IsAny<DateTimeOffset>(), "Failed to enqueue decision processing job."))
            .Returns(Task.CompletedTask);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        trackerMock.Verify(t => t.FailAsync(UserEmail, It.IsAny<DateTimeOffset>(), "Failed to enqueue decision processing job."), Times.Once);
        trackerMock.Verify(t => t.SetJobIdAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Submitting_When_Enqueue_Returns_Null_Marks_Batch_Failed_And_Returns_Processing_Unavailable()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(1);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((string _, DateTimeOffset start) => new DecisionBatchStatus { BatchId = 51, JobId = string.Empty, StartedAt = start });
        backgroundJobsMock.Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns((string)null!);
        trackerMock.Setup(t => t.FailAsync(UserEmail, It.IsAny<DateTimeOffset>(), "Decision processing job enqueue was cancelled."))
            .Returns(Task.CompletedTask);

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        trackerMock.Verify(t => t.FailAsync(UserEmail, It.IsAny<DateTimeOffset>(), "Decision processing job enqueue was cancelled."), Times.Once);
        trackerMock.Verify(t => t.SetJobIdAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Submitting_When_SetJobId_Fails_Reconciles_Using_Persisted_Batch()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(1);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((string _, DateTimeOffset start) => new DecisionBatchStatus { BatchId = 77, JobId = string.Empty, StartedAt = start });
        backgroundJobsMock.Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-77");
        trackerMock.Setup(t => t.SetJobIdAsync(77, "job-77"))
            .ThrowsAsync(new InvalidOperationException("transient SQL"));
        trackerMock.Setup(t => t.GetByBatchIdAsync(77))
            .ReturnsAsync(new DecisionBatchStatus { BatchId = 77, JobId = string.Empty, StartedAt = DateTimeOffset.UtcNow });

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeTrue();
        result.BatchStatus.Should().NotBeNull();
        result.BatchStatus!.BatchId.Should().Be(77);
        result.BatchStatus.JobId.Should().Be("job-77");
    }

    [TestMethod]
    public async Task Submitting_When_SetJobId_And_Reconciliation_Read_Fail_Returns_Processing_Unavailable()
    {
        var storeMock = new Mock<IDecisionStore>();
        var trackerMock = new Mock<IDecisionBatchTracker>();
        var executorMock = new Mock<IDecisionProcessingExecutor>();
        var backgroundJobsMock = new Mock<IBackgroundJobClient>();
        var logger = new TestLogger<DecisionSubmissionService>();

        trackerMock.Setup(t => t.FailOrphanedPendingAsync(It.IsAny<DateTimeOffset>(), "Decision processing job was not enqueued."))
            .Returns(Task.CompletedTask);
        trackerMock.Setup(t => t.GetCurrentAsync(UserEmail)).ReturnsAsync((DecisionBatchStatus?)null);
        executorMock.Setup(e => e.CanProcessAsync()).ReturnsAsync(true);
        storeMock.Setup(s => s.CountAsync(UserEmail)).ReturnsAsync(1);
        trackerMock.Setup(t => t.StartAsync(UserEmail, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((string _, DateTimeOffset start) => new DecisionBatchStatus { BatchId = 78, JobId = string.Empty, StartedAt = start });
        backgroundJobsMock.Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-78");
        trackerMock.Setup(t => t.SetJobIdAsync(78, "job-78"))
            .ThrowsAsync(new InvalidOperationException("transient SQL"));
        trackerMock.Setup(t => t.GetByBatchIdAsync(78))
            .ThrowsAsync(new InvalidOperationException("transient read SQL"));

        var service = new DecisionSubmissionService(
            storeMock.Object,
            trackerMock.Object,
            executorMock.Object,
            backgroundJobsMock.Object,
            logger);

        var result = await service.SubmitAsync(UserEmail);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        result.BatchStatus.Should().BeNull();
    }
}
