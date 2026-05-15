namespace Clc.BibDedupe.Web.Tests.Models;

using Clc.BibDedupe.Web.Models;
using System;

[TestClass]
public class DecisionSubmissionResultTests
{
    [TestMethod]
    public void Started_ReturnsSuccessWithStatus()
    {
        // Arrange
        var status = new DecisionBatchStatus
        {
            JobId = "job-123",
            StartedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = DecisionSubmissionResult.Started(status);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.BatchStatus.Should().Be(status);
    }

    [TestMethod]
    public void AlreadyInProgress_ReturnsFailureWithStatusAndMessage()
    {
        // Arrange
        var status = new DecisionBatchStatus
        {
            JobId = "job-123",
            StartedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = DecisionSubmissionResult.AlreadyInProgress(status);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("A batch is already being processed.");
        result.BatchStatus.Should().Be(status);
    }

    [TestMethod]
    public void NoDecisions_ReturnsFailureWithMessage()
    {
        // Act
        var result = DecisionSubmissionResult.NoDecisions();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("There are no decisions to submit.");
        result.BatchStatus.Should().BeNull();
    }

    [TestMethod]
    public void ProcessingUnavailable_ReturnsFailureWithMessage()
    {
        // Act
        var result = DecisionSubmissionResult.ProcessingUnavailable();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Decision processing is not available.");
        result.BatchStatus.Should().BeNull();
    }
}
