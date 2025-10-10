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
        var existing = items.FirstOrDefault(d =>
            d.Pair.LeftBibId == decision.Pair.LeftBibId &&
            d.Pair.RightBibId == decision.Pair.RightBibId);
        if (existing is not null)
        {
            existing.Action = decision.Action;
            existing.Pair = decision.Pair.Clone();
        }
        else
        {
            items.Add(CloneDecision(decision));
        }

        DecisionConflictValidator.EnsureNoMergeConflicts(items);

        Save(items);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DecisionItem>> GetAllAsync(string userId) =>
        Task.FromResult<IEnumerable<DecisionItem>>(Load());

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId)
    {
        var items = Load();
        items.RemoveAll(d => d.Pair.LeftBibId == leftBibId && d.Pair.RightBibId == rightBibId);
        Save(items);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public Task<int> CountAsync(string userId) => Task.FromResult(Load().Count);

    public Task<DecisionItem?> GetAsync(string userId, int leftBibId, int rightBibId)
    {
        var items = Load();
        var decision = items.FirstOrDefault(d => d.Pair.LeftBibId == leftBibId && d.Pair.RightBibId == rightBibId);
        return Task.FromResult(decision is null ? null : CloneDecision(decision));
    }

    private static DecisionItem CloneDecision(DecisionItem decision) => new()
    {
        Pair = decision.Pair.Clone(),
        Action = decision.Action
    };
}
