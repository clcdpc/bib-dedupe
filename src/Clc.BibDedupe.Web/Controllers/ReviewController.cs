using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Extensions;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
[Route("review")]
public class ReviewController(
    IBibDupePairRepository repository,
    IDecisionStore decisionStore,
    ICurrentPairStore currentPairStore,
    IPairAssignmentStore pairAssignmentStore,
    IReviewPageService reviewPageService,
    IPostDecisionNavigationService postDecisionNavigationService) : Controller
{
    [HttpGet]
    [HttpGet("{leftBibId:int}/{rightBibId:int}")]
    public async Task<IActionResult> Index(int? leftBibId, int? rightBibId)
    {
        var userEmail = User.GetEmail();
        var reviewPage = await reviewPageService.BuildAsync(userEmail, leftBibId, rightBibId);

        if (reviewPage is null)
        {
            await currentPairStore.ClearAsync(userEmail);
            return View(new IndexViewModel());
        }

        // Keep session/assignment lifecycle explicit at the controller boundary.
        await currentPairStore.SetAsync(userEmail, new CurrentPair
        {
            LeftBibId = reviewPage.LeftBibId,
            RightBibId = reviewPage.RightBibId
        });

        await pairAssignmentStore.AssignAsync(userEmail, reviewPage.LeftBibId, reviewPage.RightBibId);

        return View(reviewPage.Model);
    }

    [HttpPost("resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(
        [FromForm] string action,
        [FromForm] int leftBibId,
        [FromForm] int rightBibId)
    {
        if (!Enum.TryParse(action, out BibDupePairAction parsed))
        {
            return BadRequest();
        }

        var userEmail = User.GetEmail();
        var pair = await repository.GetByBibIdsAsync(leftBibId, rightBibId, userEmail);
        var decision = pair is not null
            ? CreateDecisionFromPair(pair, parsed)
            : await decisionStore.GetAsync(userEmail, leftBibId, rightBibId);

        if (decision is null)
        {
            await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
            await currentPairStore.ClearAsync(userEmail);
            return Conflict(new { error = "Pair is not available for this user." });
        }

        var isReReview = pair is null;
        decision.Action = parsed;

        try
        {
            await decisionStore.AddAsync(userEmail, decision);
        }
        catch (DecisionConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        finally
        {
            await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
            await currentPairStore.ClearAsync(userEmail);
        }

        var navigation = await postDecisionNavigationService.GetNavigationAsync(
            userEmail,
            isReReview,
            (leftBibId, rightBibId),
            (nextLeftBibId, nextRightBibId) => Url.Action(nameof(Index), new { leftBibId = nextLeftBibId, rightBibId = nextRightBibId }),
            () => Url.Action(nameof(Index)),
            () => Url.Action("Index", "Decisions"),
            () => Url.Action("Index", "Pairs"));

        return Ok(new
        {
            nextPairUrl = navigation.NextPairUrl,
            hasNextPair = navigation.HasNextPair,
            reReview = navigation.ReReview
        });
    }

    private static PairDecision CreateDecisionFromPair(BibDupePair pair, BibDupePairAction action) => new()
    {
        Pair = pair.Clone(),
        Action = action
    };
}
