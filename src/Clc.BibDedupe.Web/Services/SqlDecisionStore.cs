using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionStore(IDbConnection db) : IDecisionStore
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
        const string query = @"SELECT LeftBibId, RightBibId, ActionId, PrimaryMarcTomId,
                                         LeftTitle, LeftAuthor, RightTitle, RightAuthor,
                                         TOM, MatchesJson
                                  FROM BibDedupe.GetDecisionQueue(@UserEmail)";

        var rows = (await db.QueryAsync<DecisionRow>(
            query,
            new
            {
                UserEmail = userId
            })).ToList();

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

    public async Task<DecisionItem?> GetAsync(string userId, int leftBibId, int rightBibId)
    {
        const string query = @"SELECT LeftBibId, RightBibId, ActionId, PrimaryMarcTomId,
                                         LeftTitle, LeftAuthor, RightTitle, RightAuthor,
                                         TOM, MatchesJson
                                  FROM BibDedupe.GetDecisionQueue(@UserEmail)
                                  WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId";

        var row = await db.QueryFirstOrDefaultAsync<DecisionRow>(
            query,
            new
            {
                UserEmail = userId,
                LeftBibId = leftBibId,
                RightBibId = rightBibId
            });

        if (row is null)
        {
            return null;
        }

        return row.PrimaryMarcTomId.HasValue
            ? MapRow(row)
            : new DecisionItem
            {
                LeftBibId = row.LeftBibId,
                RightBibId = row.RightBibId,
                Action = (BibDupePairAction)row.ActionId
            };
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
