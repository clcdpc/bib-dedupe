using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionStore(IDbConnection db, IPairFilterStore pairFilterStore) : IDecisionStore
{
    private const string Table = "BibDedupe.DecisionQueue";

    public async Task AddAsync(string userId, DecisionItem decision)
    {
        var decisions = await LoadDecisionSummariesAsync(userId);
        var existing = decisions.FirstOrDefault(d => d.LeftBibId == decision.LeftBibId && d.RightBibId == decision.RightBibId);
        if (existing is not null)
        {
            existing.Action = decision.Action;
        }
        else
        {
            decisions.Add(new DecisionItem
            {
                LeftBibId = decision.LeftBibId,
                RightBibId = decision.RightBibId,
                Action = decision.Action
            });
        }

        DecisionConflictValidator.EnsureNoMergeConflicts(decisions);

        var parameters = new
        {
            UserEmail = userId,
            decision.LeftBibId,
            decision.RightBibId,
            ActionId = (int)decision.Action
        };

        var updated = await db.ExecuteAsync($"UPDATE {Table} SET ActionId = @ActionId WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId", parameters);

        if (updated == 0)
        {
            await db.ExecuteAsync($"INSERT INTO {Table}(UserEmail, LeftBibId, RightBibId, ActionId) VALUES (@UserEmail, @LeftBibId, @RightBibId, @ActionId)", parameters);
        }
    }

    public async Task<IEnumerable<DecisionItem>> GetAllAsync(string userId)
    {
        var summaries = await LoadDecisionSummariesAsync(userId);
        if (summaries.Count == 0)
        {
            return summaries;
        }

        var filters = await pairFilterStore.GetAsync(userId);
        var parameters = new
        {
            UserEmail = userId,
            Top = int.MaxValue,
            TomId = filters?.TomId,
            MatchType = filters?.MatchType,
            HasHolds = filters?.HasHolds
        };

        var rows = await db.QueryAsync<DecisionRow>(
            $@"SELECT d.LeftBibId, d.RightBibId, d.ActionId, p.PrimaryMARCTOMID AS PrimaryMarcTomId,
                      p.LeftTitle, p.LeftAuthor, p.RightTitle, p.RightAuthor,
                      p.TOM, p.MatchesJson
               FROM {Table} d
               JOIN BibDedupe.GetPairs(@Top, NULL, @TomId, @MatchType, @HasHolds) p ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
               WHERE d.UserEmail = @UserEmail",
            parameters);

        var rowsByPair = rows.ToDictionary(r => (r.LeftBibId, r.RightBibId));

        if (rowsByPair.Count < summaries.Count && filters is not null && !filters.IsEmpty)
        {
            var fallbackRows = await db.QueryAsync<DecisionRow>(
                $@"SELECT d.LeftBibId, d.RightBibId, d.ActionId, p.PrimaryMARCTOMID AS PrimaryMarcTomId,
                          p.LeftTitle, p.LeftAuthor, p.RightTitle, p.RightAuthor,
                          p.TOM, p.MatchesJson
                   FROM {Table} d
                   JOIN BibDedupe.GetPairs(@Top, NULL, NULL, NULL, NULL) p ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
                   WHERE d.UserEmail = @UserEmail",
                new { UserEmail = userId, Top = int.MaxValue });

            foreach (var row in fallbackRows)
            {
                rowsByPair.TryAdd((row.LeftBibId, row.RightBibId), row);
            }
        }

        var decisions = new List<DecisionItem>(summaries.Count);

        foreach (var summary in summaries)
        {
            if (rowsByPair.TryGetValue((summary.LeftBibId, summary.RightBibId), out var row))
            {
                decisions.Add(MapRow(row));
            }
            else
            {
                decisions.Add(new DecisionItem
                {
                    LeftBibId = summary.LeftBibId,
                    RightBibId = summary.RightBibId,
                    Action = summary.Action
                });
            }
        }

        return decisions;
    }

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync($"DELETE FROM {Table} WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public async Task<int> CountAsync(string userId) =>
        await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });

    private static DecisionItem MapRow(DecisionRow row) => new()
    {
        LeftBibId = row.LeftBibId,
        RightBibId = row.RightBibId,
        LeftTitle = row.LeftTitle,
        LeftAuthor = row.LeftAuthor,
        RightTitle = row.RightTitle,
        RightAuthor = row.RightAuthor,
        TOM = row.TOM,
        PrimaryMarcTomId = row.PrimaryMarcTomId,
        Action = (BibDupePairAction)row.ActionId,
        Matches = PairMatch.FromJson(row.MatchesJson)
    };

    private sealed class DecisionRow
    {
        public int LeftBibId { get; init; }
        public int RightBibId { get; init; }
        public int ActionId { get; init; }
        public int PrimaryMarcTomId { get; init; }
        public string? LeftTitle { get; init; }
        public string? LeftAuthor { get; init; }
        public string? RightTitle { get; init; }
        public string? RightAuthor { get; init; }
        public string? TOM { get; init; }
        public string MatchesJson { get; init; } = string.Empty;
    }

    private async Task<List<DecisionItem>> LoadDecisionSummariesAsync(string userId)
    {
        var rows = await db.QueryAsync<DecisionSummaryRow>(
            $"SELECT LeftBibId, RightBibId, ActionId FROM {Table} WHERE UserEmail = @UserEmail",
            new { UserEmail = userId });

        return rows.Select(r => new DecisionItem
        {
            LeftBibId = r.LeftBibId,
            RightBibId = r.RightBibId,
            Action = (BibDupePairAction)r.ActionId
        }).ToList();
    }

    private sealed class DecisionSummaryRow
    {
        public int LeftBibId { get; init; }
        public int RightBibId { get; init; }
        public int ActionId { get; init; }
    }
}
