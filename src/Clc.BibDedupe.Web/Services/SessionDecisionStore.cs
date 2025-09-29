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
        var existing = items.FirstOrDefault(d => d.LeftBibId == decision.LeftBibId && d.RightBibId == decision.RightBibId);
        if (existing is not null)
        {
            existing.Action = decision.Action;
            existing.PrimaryMarcTomId = decision.PrimaryMarcTomId;
            existing.Matches = CloneMatches(decision.Matches);
            existing.LeftTitle = decision.LeftTitle;
            existing.LeftAuthor = decision.LeftAuthor;
            existing.RightTitle = decision.RightTitle;
            existing.RightAuthor = decision.RightAuthor;
            existing.TOM = decision.TOM;
        }
        else
        {
            items.Add(CloneDecision(decision));
        }
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
