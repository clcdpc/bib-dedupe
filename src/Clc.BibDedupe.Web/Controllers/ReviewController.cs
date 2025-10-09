using System;
using System.Collections.Generic;
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
        BibDupePair? pair;
        DecisionItem? existingDecision = null;
        if (leftBibId is null || rightBibId is null)
        {
            var filters = await pairFilterStore.GetAsync(userEmail);
            pair = (await repository.GetAsync(
                userEmail,
                filters?.TomId,
                filters?.MatchType,
                filters?.HasHolds)).FirstOrDefault();
            if (pair is null)
            {
                await currentPairStore.ClearAsync(userEmail);
                return View(new IndexViewModel());
            }
            leftBibId = pair.LeftBibId;
            rightBibId = pair.RightBibId;
        }
        else
        {
            pair = await repository.GetByBibIdsAsync(leftBibId.Value, rightBibId.Value, userEmail);
            if (pair is null)
            {
                existingDecision = (await decisionStore.GetAllAsync(userEmail))
                    .FirstOrDefault(d => d.LeftBibId == leftBibId && d.RightBibId == rightBibId);
            }
        }

        var (left, right) = await loader.LoadAsync(leftBibId.Value, rightBibId.Value);

        var model = new IndexViewModel
        {
            LeftBibId = leftBibId.Value,
            RightBibId = rightBibId.Value,
            LeftTitle = pair?.LeftTitle ?? existingDecision?.LeftTitle,
            RightTitle = pair?.RightTitle ?? existingDecision?.RightTitle,
            LeftBibXml = MarcXmlRenderer.TransformFile(left.BibXml, "marc-to-html.xslt"),
            RightBibXml = MarcXmlRenderer.TransformFile(right.BibXml, "marc-to-html.xslt"),
            LeftItems = left.Items,
            RightItems = right.Items,
            Matches = pair?.Matches
                .Select(m => new PairMatch
                {
                    MatchType = m.MatchType,
                    MatchValue = m.MatchValue
                })
                .ToList()
                ?? existingDecision?.Matches
                    .Select(m => new PairMatch
                    {
                        MatchType = m.MatchType,
                        MatchValue = m.MatchValue
                    })
                    .ToList()
                ?? new List<PairMatch>(),
            LeftHoldCount = pair?.LeftHoldCount ?? 0,
            RightHoldCount = pair?.RightHoldCount ?? 0,
            TotalHoldCount = pair?.TotalHoldCount ?? 0
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
        DecisionItem decision;
        if (pair is not null)
        {
            decision = new DecisionItem
            {
                LeftBibId = leftBibId,
                RightBibId = rightBibId,
                LeftTitle = pair.LeftTitle,
                LeftAuthor = pair.LeftAuthor,
                RightTitle = pair.RightTitle,
                RightAuthor = pair.RightAuthor,
                TOM = pair.TOM,
                PrimaryMarcTomId = pair.PrimaryMarcTomId,
                Matches = pair.Matches.Select(m => new PairMatch
                {
                    MatchType = m.MatchType,
                    MatchValue = m.MatchValue
                }).ToList(),
                Action = parsed
            };
        }
        else
        {
            var existingDecision = (await decisionStore.GetAllAsync(userEmail))
                .FirstOrDefault(d => d.LeftBibId == leftBibId && d.RightBibId == rightBibId);
            if (existingDecision is null)
            {
                await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
                await currentPairStore.ClearAsync(userEmail);
                return Conflict(new { error = "Pair is not available for this user." });
            }

            decision = existingDecision with { Action = parsed };
            isReReview = true;
        }
        try
        {
            await decisionStore.AddAsync(userEmail, decision);
        }
        catch (DecisionConflictException ex)
        {
            await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
            await currentPairStore.ClearAsync(userEmail);
            return Conflict(new { error = ex.Message });
        }

        await pairAssignmentStore.ReleaseAsync(userEmail, leftBibId, rightBibId);
        await currentPairStore.ClearAsync(userEmail);

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
}
