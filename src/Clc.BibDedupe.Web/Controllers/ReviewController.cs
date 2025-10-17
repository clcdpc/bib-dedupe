using System;
using System.Linq;
using System.Threading.Tasks;
using Clc.BibDedupe.Web;
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
    IRecordLoader loader,
    IBibDupePairRepository repository,
    IDecisionStore decisionStore,
    ICurrentPairStore currentPairStore,
    IPairAssignmentStore pairAssignmentStore,
    IPairFilterStore pairFilterStore) : Controller
{
    [HttpGet]
    [HttpGet("{leftBibId:int}/{rightBibId:int}")]
    public async Task<IActionResult> Index(int? leftBibId, int? rightBibId)
    {
        var userEmail = User.GetEmail();
        var reviewPair = await GetReviewPairAsync(userEmail, leftBibId, rightBibId);

        if (reviewPair is null)
        {
            await currentPairStore.ClearAsync(userEmail);
            return View(new IndexViewModel());
        }

        var (leftRecord, rightRecord) = await loader.LoadAsync(reviewPair.LeftBibId, reviewPair.RightBibId);
        var validActions = await repository.GetValidActionsAsync(reviewPair.LeftBibId, reviewPair.RightBibId, userEmail);

        var model = new IndexViewModel
        {
            LeftBibId = reviewPair.LeftBibId,
            RightBibId = reviewPair.RightBibId,
            LeftTitle = reviewPair.Pair.LeftTitle,
            RightTitle = reviewPair.Pair.RightTitle,
            LeftBibXml = MarcXmlRenderer.TransformFile(leftRecord.BibXml, "marc-to-html.xslt"),
            RightBibXml = MarcXmlRenderer.TransformFile(rightRecord.BibXml, "marc-to-html.xslt"),
            LeftItems = leftRecord.Items,
            RightItems = rightRecord.Items,
            Matches = PairMatch.CloneList(reviewPair.Pair.Matches),
            LeftHoldCount = reviewPair.Pair.LeftHoldCount,
            RightHoldCount = reviewPair.Pair.RightHoldCount,
            TotalHoldCount = reviewPair.Pair.TotalHoldCount,
            ValidActions = validActions.ToHashSet()
        };

        await currentPairStore.SetAsync(userEmail, new CurrentPair
        {
            LeftBibId = model.LeftBibId,
            RightBibId = model.RightBibId
        });

        await pairAssignmentStore.AssignAsync(userEmail, model.LeftBibId, model.RightBibId);

        return View(model);
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

        var isReReview = false;
        var decision = pair is not null
            ? CreateDecisionFromPair(pair, parsed)
            : await decisionStore.GetAsync(userEmail, leftBibId, rightBibId);

        if (decision is null)
        {
            await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
            await currentPairStore.ClearAsync(userEmail);
            return Conflict(new { error = "Pair is not available for this user." });
        }

        if (pair is null)
        {
            isReReview = true;
        }

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

        string? nextPairUrl = null;
        bool hasNextPair = false;

        if (isReReview)
        {
            nextPairUrl = Url.Action("Index", "Decisions");
        }
        else
        {
            var filters = await pairFilterStore.GetAsync(userEmail);
            var nextPair = (await repository.GetAsync(
                    userEmail,
                    filters?.TomId,
                    filters?.MatchType,
                    filters?.HasHolds))
                .FirstOrDefault(p => p.LeftBibId != leftBibId || p.RightBibId != rightBibId);

            nextPairUrl = nextPair is not null
                ? Url.Action(
                    nameof(Index),
                    new
                    {
                        leftBibId = nextPair.LeftBibId,
                        rightBibId = nextPair.RightBibId
                    })
                : Url.Action(nameof(Index));

            hasNextPair = nextPair is not null;
        }

        nextPairUrl ??= Url.Action("Index", "Pairs");
        nextPairUrl ??= "/";

        return Ok(new
        {
            nextPairUrl,
            hasNextPair,
            reReview = isReReview
        });
    }

    private async Task<ReviewPair?> GetReviewPairAsync(string userEmail, int? leftBibId, int? rightBibId)
    {
        if (leftBibId is null || rightBibId is null)
        {
            var filters = await pairFilterStore.GetAsync(userEmail);
            var nextPair = (await repository.GetAsync(
                    userEmail,
                    filters?.TomId,
                    filters?.MatchType,
                    filters?.HasHolds))
                .FirstOrDefault();

            return nextPair is null
                ? null
                : new ReviewPair(
                    nextPair.LeftBibId,
                    nextPair.RightBibId,
                    nextPair.Clone(),
                    null);
        }

        var pair = await repository.GetByBibIdsAsync(leftBibId.Value, rightBibId.Value, userEmail);
        if (pair is not null)
        {
            return new ReviewPair(leftBibId.Value, rightBibId.Value, pair.Clone(), null);
        }

        var existingDecision = await decisionStore.GetAsync(userEmail, leftBibId.Value, rightBibId.Value);
        return existingDecision is null
            ? null
            : new ReviewPair(
                leftBibId.Value,
                rightBibId.Value,
                existingDecision.Pair.Clone(),
                new ReviewDecision(existingDecision.Action));
    }

    private static PairDecision CreateDecisionFromPair(BibDupePair pair, BibDupePairAction action) => new()
    {
        Pair = pair.Clone(),
        Action = action
    };

    private sealed record ReviewPair(
        int LeftBibId,
        int RightBibId,
        BibDupePair Pair,
        ReviewDecision? ExistingDecision);

    private sealed record ReviewDecision(BibDupePairAction Action);
}
