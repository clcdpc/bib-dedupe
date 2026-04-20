using System.Collections.Generic;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class ReviewPageService(
    IRecordLoader loader,
    IBibDupePairRepository repository,
    IDecisionStore decisionStore,
    INextPairResolver nextPairResolver) : IReviewPageService
{
    public async Task<ReviewPageResult?> BuildAsync(string userEmail, int? leftBibId, int? rightBibId)
    {
        var reviewPair = await GetReviewPairAsync(userEmail, leftBibId, rightBibId);
        if (reviewPair is null)
        {
            return null;
        }

        var (leftRecord, rightRecord) = await loader.LoadAsync(reviewPair.LeftBibId, reviewPair.RightBibId);
        var validActions = await repository.GetValidActionsAsync(reviewPair.LeftBibId, reviewPair.RightBibId, userEmail);

        var existingDecisionAction = reviewPair.ExistingDecision?.Action;
        var isReReview = existingDecisionAction is not null;

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
            ValidActions = validActions.ToHashSet(),
            IsReReview = isReReview,
            ExistingDecisionAction = existingDecisionAction
        };

        return new ReviewPageResult
        {
            LeftBibId = reviewPair.LeftBibId,
            RightBibId = reviewPair.RightBibId,
            Model = model
        };
    }

    private async Task<ReviewPair?> GetReviewPairAsync(string userEmail, int? leftBibId, int? rightBibId)
    {
        if (leftBibId is null || rightBibId is null)
        {
            var nextPair = await nextPairResolver.GetNextPairForUserAsync(userEmail, filters: null);

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

    private sealed record ReviewPair(
        int LeftBibId,
        int RightBibId,
        BibDupePair Pair,
        ReviewDecision? ExistingDecision);

    private sealed record ReviewDecision(BibDupePairAction Action);
}
