using System.Linq;
using System.Security.Claims;
using Clc.BibDedupe.Web.Controllers;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Controllers;

[TestClass]
public class ReviewControllerTests
{
    private const string UserEmail = "user@example.com";

    [TestMethod]
    public async Task Index_Assigns_And_Stores_Current_Pair_On_Successful_Load()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var decisionStoreMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var currentPairStoreMock = new Mock<ICurrentPairStore>(MockBehavior.Strict);
        var pairAssignmentStoreMock = new Mock<IPairAssignmentStore>(MockBehavior.Strict);
        var reviewPageServiceMock = new Mock<IReviewPageService>(MockBehavior.Strict);
        var navigationServiceMock = new Mock<IPostDecisionNavigationService>(MockBehavior.Strict);

        reviewPageServiceMock
            .Setup(s => s.BuildAsync(UserEmail, null, null))
            .ReturnsAsync(new ReviewPageResult
            {
                LeftBibId = 10,
                RightBibId = 20,
                Model = new IndexViewModel { LeftBibId = 10, RightBibId = 20 }
            });

        currentPairStoreMock
            .Setup(s => s.SetAsync(UserEmail, It.Is<CurrentPair>(p => p.LeftBibId == 10 && p.RightBibId == 20)))
            .Returns(Task.CompletedTask);
        pairAssignmentStoreMock.Setup(s => s.AssignAsync(UserEmail, 10, 20)).Returns(Task.CompletedTask);

        var controller = CreateController(
            repositoryMock,
            decisionStoreMock,
            currentPairStoreMock,
            pairAssignmentStoreMock,
            reviewPageServiceMock,
            navigationServiceMock);

        var result = await controller.Index(null, null);

        result.Should().BeOfType<ViewResult>();

        reviewPageServiceMock.VerifyAll();
        currentPairStoreMock.VerifyAll();
        pairAssignmentStoreMock.VerifyAll();
        navigationServiceMock.VerifyNoOtherCalls();
        repositoryMock.VerifyNoOtherCalls();
        decisionStoreMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Resolve_Returns_Payload_And_Releases_Pair_For_Normal_Review()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var decisionStoreMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var currentPairStoreMock = new Mock<ICurrentPairStore>(MockBehavior.Strict);
        var pairAssignmentStoreMock = new Mock<IPairAssignmentStore>(MockBehavior.Strict);
        var reviewPageServiceMock = new Mock<IReviewPageService>(MockBehavior.Strict);
        var navigationServiceMock = new Mock<IPostDecisionNavigationService>(MockBehavior.Strict);

        repositoryMock
            .Setup(r => r.GetByBibIdsAsync(10, 20, UserEmail, true))
            .ReturnsAsync(new BibDupePair { LeftBibId = 10, RightBibId = 20 });
        decisionStoreMock.Setup(s => s.AddAsync(UserEmail, It.IsAny<PairDecision>())).Returns(Task.CompletedTask);
        pairAssignmentStoreMock.Setup(s => s.ReleaseAsync(UserEmail, 10, 20)).Returns(Task.CompletedTask);
        currentPairStoreMock.Setup(s => s.ClearAsync(UserEmail)).Returns(Task.CompletedTask);

        navigationServiceMock
            .Setup(s => s.GetNavigationAsync(
                UserEmail,
                false,
                It.Is<(int leftBibId, int rightBibId)>(pair => pair.leftBibId == 10 && pair.rightBibId == 20),
                It.IsAny<Func<int, int, string?>>(),
                It.IsAny<Func<string?>>(),
                It.IsAny<Func<string?>>(),
                It.IsAny<Func<string?>>()))
            .ReturnsAsync(new PostDecisionNavigationResult
            {
                NextPairUrl = "/review/30/40",
                HasNextPair = true,
                ReReview = false
            });

        var controller = CreateController(
            repositoryMock,
            decisionStoreMock,
            currentPairStoreMock,
            pairAssignmentStoreMock,
            reviewPageServiceMock,
            navigationServiceMock);

        var result = await controller.Resolve("KeepLeft", 10, 20);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ToDictionary(ok.Value!);

        payload["nextPairUrl"].Should().Be("/review/30/40");
        payload["hasNextPair"].Should().Be(true);
        payload["reReview"].Should().Be(false);

        pairAssignmentStoreMock.VerifyAll();
        currentPairStoreMock.VerifyAll();
        repositoryMock.VerifyAll();
        decisionStoreMock.VerifyAll();
        navigationServiceMock.VerifyAll();
        reviewPageServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Resolve_ReReview_Path_Navigates_To_Decisions_Index()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var decisionStoreMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var currentPairStoreMock = new Mock<ICurrentPairStore>(MockBehavior.Strict);
        var pairAssignmentStoreMock = new Mock<IPairAssignmentStore>(MockBehavior.Strict);
        var reviewPageServiceMock = new Mock<IReviewPageService>(MockBehavior.Strict);
        var navigationServiceMock = new Mock<IPostDecisionNavigationService>(MockBehavior.Strict);

        repositoryMock
            .Setup(r => r.GetByBibIdsAsync(10, 20, UserEmail, true))
            .ReturnsAsync((BibDupePair?)null);
        decisionStoreMock
            .Setup(s => s.GetAsync(UserEmail, 10, 20))
            .ReturnsAsync(new PairDecision { Pair = new BibDupePair { LeftBibId = 10, RightBibId = 20 }, Action = BibDupePairAction.Skip });
        decisionStoreMock.Setup(s => s.AddAsync(UserEmail, It.IsAny<PairDecision>())).Returns(Task.CompletedTask);

        pairAssignmentStoreMock.Setup(s => s.ReleaseAsync(UserEmail, 10, 20)).Returns(Task.CompletedTask);
        currentPairStoreMock.Setup(s => s.ClearAsync(UserEmail)).Returns(Task.CompletedTask);

        navigationServiceMock
            .Setup(s => s.GetNavigationAsync(
                UserEmail,
                true,
                It.Is<(int leftBibId, int rightBibId)>(pair => pair.leftBibId == 10 && pair.rightBibId == 20),
                It.IsAny<Func<int, int, string?>>(),
                It.IsAny<Func<string?>>(),
                It.IsAny<Func<string?>>(),
                It.IsAny<Func<string?>>()))
            .ReturnsAsync(new PostDecisionNavigationResult
            {
                NextPairUrl = "/decisions",
                HasNextPair = false,
                ReReview = true
            });

        var controller = CreateController(
            repositoryMock,
            decisionStoreMock,
            currentPairStoreMock,
            pairAssignmentStoreMock,
            reviewPageServiceMock,
            navigationServiceMock);

        var result = await controller.Resolve("KeepLeft", 10, 20);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ToDictionary(ok.Value!);
        payload["nextPairUrl"].Should().Be("/decisions");
        payload["hasNextPair"].Should().Be(false);
        payload["reReview"].Should().Be(true);
    }

    [TestMethod]
    public async Task Resolve_Conflict_Still_Releases_And_Clears_Current_Pair()
    {
        var repositoryMock = new Mock<IBibDupePairRepository>(MockBehavior.Strict);
        var decisionStoreMock = new Mock<IDecisionStore>(MockBehavior.Strict);
        var currentPairStoreMock = new Mock<ICurrentPairStore>(MockBehavior.Strict);
        var pairAssignmentStoreMock = new Mock<IPairAssignmentStore>(MockBehavior.Strict);
        var reviewPageServiceMock = new Mock<IReviewPageService>(MockBehavior.Strict);
        var navigationServiceMock = new Mock<IPostDecisionNavigationService>(MockBehavior.Strict);

        repositoryMock
            .Setup(r => r.GetByBibIdsAsync(10, 20, UserEmail, true))
            .ReturnsAsync(new BibDupePair { LeftBibId = 10, RightBibId = 20 });

        decisionStoreMock
            .Setup(s => s.AddAsync(UserEmail, It.IsAny<PairDecision>()))
            .ThrowsAsync(new DecisionConflictException(10, "conflict"));

        pairAssignmentStoreMock.Setup(s => s.ReleaseAsync(UserEmail, 10, 20)).Returns(Task.CompletedTask);
        currentPairStoreMock.Setup(s => s.ClearAsync(UserEmail)).Returns(Task.CompletedTask);

        var controller = CreateController(
            repositoryMock,
            decisionStoreMock,
            currentPairStoreMock,
            pairAssignmentStoreMock,
            reviewPageServiceMock,
            navigationServiceMock);

        var result = await controller.Resolve("KeepLeft", 10, 20);

        result.Should().BeOfType<ConflictObjectResult>();
        pairAssignmentStoreMock.Verify(s => s.ReleaseAsync(UserEmail, 10, 20), Times.Once);
        currentPairStoreMock.Verify(s => s.ClearAsync(UserEmail), Times.Once);
        navigationServiceMock.VerifyNoOtherCalls();
    }

    private static ReviewController CreateController(
        Mock<IBibDupePairRepository> repositoryMock,
        Mock<IDecisionStore> decisionStoreMock,
        Mock<ICurrentPairStore> currentPairStoreMock,
        Mock<IPairAssignmentStore> pairAssignmentStoreMock,
        Mock<IReviewPageService> reviewPageServiceMock,
        Mock<IPostDecisionNavigationService> navigationServiceMock)
    {
        var controller = new ReviewController(
            repositoryMock.Object,
            decisionStoreMock.Object,
            currentPairStoreMock.Object,
            pairAssignmentStoreMock.Object,
            reviewPageServiceMock.Object,
            navigationServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Email, UserEmail) },
                        authenticationType: "test"))
                }
            }
        };

        return controller;
    }

    private static Dictionary<string, object?> ToDictionary(object value)
    {
        return value.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(value));
    }
}
