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
        var filters = await pairFilterStore.GetAsync(userId);
        var hasFilters = filters is not null && !filters.IsEmpty;

        var parameters = new
        {
            UserEmail = userId,
            Top = int.MaxValue,
            TomId = filters?.TomId,
            MatchType = filters?.MatchType,
            HasHolds = filters?.HasHolds
        };

        var sql = hasFilters ? FilteredDecisionsSql : UnfilteredDecisionsSql;
        var rows = (await db.QueryAsync<DecisionRow>(sql, parameters)).ToList();

        var decisions = new List<DecisionItem>(rows.Count);

        foreach (var row in rows)
        {
            if (row.PrimaryMarcTomId.HasValue)
            {
                decisions.Add(MapRow(row));
            }
            else
            {
                decisions.Add(new DecisionItem
                {
                    LeftBibId = row.LeftBibId,
                    RightBibId = row.RightBibId,
                    Action = (BibDupePairAction)row.ActionId
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
        PrimaryMarcTomId = row.PrimaryMarcTomId!.Value,
        Action = (BibDupePairAction)row.ActionId,
        Matches = PairMatch.FromJson(row.MatchesJson)
    };

    private sealed class DecisionRow
    {
        public int LeftBibId { get; init; }
        public int RightBibId { get; init; }
        public int ActionId { get; init; }
        public int? PrimaryMarcTomId { get; init; }
        public string? LeftTitle { get; init; }
        public string? LeftAuthor { get; init; }
        public string? RightTitle { get; init; }
        public string? RightAuthor { get; init; }
        public string? TOM { get; init; }
        public string? MatchesJson { get; init; }
    }

    private static readonly string UnfilteredDecisionsSql = $@"
WITH UserDecisions AS (
    SELECT LeftBibId, RightBibId, ActionId
    FROM {Table}
    WHERE UserEmail = @UserEmail
)
SELECT d.LeftBibId, d.RightBibId, d.ActionId,
       p.PrimaryMARCTOMID AS PrimaryMarcTomId,
       p.LeftTitle, p.LeftAuthor, p.RightTitle, p.RightAuthor,
       p.TOM, p.MatchesJson
FROM UserDecisions d
LEFT JOIN BibDedupe.GetPairs(@Top, NULL, NULL, NULL, NULL) p
    ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId";

    private static readonly string FilteredDecisionsSql = $@"
WITH UserDecisions AS (
    SELECT LeftBibId, RightBibId, ActionId
    FROM {Table}
    WHERE UserEmail = @UserEmail
),
FilteredPairs AS (
    SELECT p.*
    FROM BibDedupe.GetPairs(@Top, NULL, @TomId, @MatchType, @HasHolds) p
    JOIN UserDecisions d ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
),
FallbackPairs AS (
    SELECT p.*
    FROM BibDedupe.GetPairs(@Top, NULL, NULL, NULL, NULL) p
    JOIN UserDecisions d ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
    WHERE NOT EXISTS (
        SELECT 1
        FROM FilteredPairs fp
        WHERE fp.LeftBibId = d.LeftBibId AND fp.RightBibId = d.RightBibId
    )
)
SELECT d.LeftBibId, d.RightBibId, d.ActionId,
       COALESCE(fp.PrimaryMARCTOMID, fb.PrimaryMARCTOMID) AS PrimaryMarcTomId,
       COALESCE(fp.LeftTitle, fb.LeftTitle) AS LeftTitle,
       COALESCE(fp.LeftAuthor, fb.LeftAuthor) AS LeftAuthor,
       COALESCE(fp.RightTitle, fb.RightTitle) AS RightTitle,
       COALESCE(fp.RightAuthor, fb.RightAuthor) AS RightAuthor,
       COALESCE(fp.TOM, fb.TOM) AS TOM,
       COALESCE(fp.MatchesJson, fb.MatchesJson) AS MatchesJson
FROM UserDecisions d
LEFT JOIN FilteredPairs fp ON fp.LeftBibId = d.LeftBibId AND fp.RightBibId = d.RightBibId
LEFT JOIN FallbackPairs fb ON fb.LeftBibId = d.LeftBibId AND fb.RightBibId = d.RightBibId";

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
