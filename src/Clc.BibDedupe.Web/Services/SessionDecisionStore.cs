using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Services;

public class SessionDecisionStore(IHttpContextAccessor accessor) : IDecisionStore
{
    private const string SessionKey = "DecisionCart";
    private ISession Session => accessor.HttpContext!.Session;

    private List<DecisionItem> Load()
    {
        var json = Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json)
            ? new List<DecisionItem>()
            : JsonSerializer.Deserialize<List<DecisionItem>>(json) ?? new List<DecisionItem>();
    }

    private void Save(List<DecisionItem> items) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(items));

    public Task AddAsync(string userId, DecisionItem decision)
    {
        var items = Load();
        items.RemoveAll(d => d.LeftBibId == decision.LeftBibId && d.RightBibId == decision.RightBibId);

        EnsureNoMergeConflicts(decision, items);

        items.Add(CloneDecision(decision));
        Save(items);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DecisionItem>> GetAllAsync(string userId) =>
        Task.FromResult<IEnumerable<DecisionItem>>(Load());

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId)
    {
        var items = Load();
        items.RemoveAll(d => d.LeftBibId == leftBibId && d.RightBibId == rightBibId);
        Save(items);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public Task<int> CountAsync(string userId) => Task.FromResult(Load().Count);

    private static void EnsureNoMergeConflicts(DecisionItem decision, IEnumerable<DecisionItem> existing)
    {
        if (!TryGetKeepDelete(decision, out var current))
        {
            return;
        }

        foreach (var item in existing)
        {
            if (!TryGetKeepDelete(item, out var other))
            {
                continue;
            }

            if (other.KeepBibId == current.DeleteBibId)
            {
                throw new ConflictingMergeDecisionException(
                    $"Cannot delete bib {current.DeleteBibId} because it is already set to keep bib {other.DeleteBibId}.");
            }

            if (other.DeleteBibId == current.KeepBibId)
            {
                throw new ConflictingMergeDecisionException(
                    $"Cannot keep bib {current.KeepBibId} because it is already marked to be merged into bib {other.KeepBibId}.");
            }
        }
    }

    private static bool TryGetKeepDelete(DecisionItem decision, out (int KeepBibId, int DeleteBibId) result)
    {
        switch (decision.Action)
        {
            case BibDupePairAction.KeepLeft:
                result = (decision.LeftBibId, decision.RightBibId);
                return true;
            case BibDupePairAction.KeepRight:
                result = (decision.RightBibId, decision.LeftBibId);
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static DecisionItem CloneDecision(DecisionItem decision) => new()
    {
        LeftBibId = decision.LeftBibId,
        RightBibId = decision.RightBibId,
        LeftTitle = decision.LeftTitle,
        LeftAuthor = decision.LeftAuthor,
        RightTitle = decision.RightTitle,
        RightAuthor = decision.RightAuthor,
        TOM = decision.TOM,
        Matches = CloneMatches(decision.Matches),
        PrimaryMarcTomId = decision.PrimaryMarcTomId,
        Action = decision.Action
    };

    private static List<PairMatch> CloneMatches(IEnumerable<PairMatch> matches) =>
        matches.Select(m => new PairMatch
        {
            MatchType = m.MatchType,
            MatchValue = m.MatchValue
        }).ToList();
}
