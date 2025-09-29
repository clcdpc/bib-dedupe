using System.Data;
using System.Linq;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionStore(IDbConnection db) : IDecisionStore
{
    private const string Table = "BibDedupe.DecisionQueue";

    public async Task AddAsync(string userId, DecisionItem decision)
    {
        var parameters = new
        {
            UserEmail = userId,
            decision.LeftBibId,
            decision.RightBibId,
            ActionId = (int)decision.Action
        };

        await EnsureNoMergeConflictsAsync(userId, decision);

        var updated = await db.ExecuteAsync($"UPDATE {Table} SET ActionId = @ActionId WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId", parameters);

        if (updated == 0)
        {
            await db.ExecuteAsync($"INSERT INTO {Table}(UserEmail, LeftBibId, RightBibId, ActionId) VALUES (@UserEmail, @LeftBibId, @RightBibId, @ActionId)", parameters);
        }
    }

    public async Task<IEnumerable<DecisionItem>> GetAllAsync(string userId)
    {
        var rows = await db.QueryAsync<DecisionRow>(
            $@"SELECT d.LeftBibId, d.RightBibId, d.ActionId, p.PrimaryMARCTOMID AS PrimaryMarcTomId,
                      p.LeftTitle, p.LeftAuthor, p.RightTitle, p.RightAuthor,
                      p.TOM, p.MatchesJson
               FROM {Table} d
               JOIN BibDedupe.GetPairs(@Top) p ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
               WHERE d.UserEmail = @UserEmail",
            new { UserEmail = userId, Top = int.MaxValue });

        return rows.Select(MapRow).ToList();
    }

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync($"DELETE FROM {Table} WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public async Task<int> CountAsync(string userId) =>
        await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });

    private async Task EnsureNoMergeConflictsAsync(string userId, DecisionItem decision)
    {
        if (!TryGetKeepDelete(decision, out var current))
        {
            return;
        }

        var deleteConflict = await db.QuerySingleOrDefaultAsync<DecisionRow>(
            $@"SELECT TOP (1) LeftBibId, RightBibId, ActionId
               FROM {Table}
               WHERE UserEmail = @UserEmail
                 AND ActionId IN (1, 4)
                 AND NOT (LeftBibId = @LeftBibId AND RightBibId = @RightBibId)
                 AND (CASE WHEN ActionId = 1 THEN LeftBibId ELSE RightBibId END) = @DeleteBibId",
            new
            {
                UserEmail = userId,
                decision.LeftBibId,
                decision.RightBibId,
                DeleteBibId = current.DeleteBibId
            });

        if (deleteConflict is not null)
        {
            var otherDeleteBibId = deleteConflict.ActionId == (int)BibDupePairAction.KeepLeft
                ? deleteConflict.RightBibId
                : deleteConflict.LeftBibId;
            throw new ConflictingMergeDecisionException(
                $"Cannot delete bib {current.DeleteBibId} because it is already set to keep bib {otherDeleteBibId}.");
        }

        var keepConflict = await db.QuerySingleOrDefaultAsync<DecisionRow>(
            $@"SELECT TOP (1) LeftBibId, RightBibId, ActionId
               FROM {Table}
               WHERE UserEmail = @UserEmail
                 AND ActionId IN (1, 4)
                 AND NOT (LeftBibId = @LeftBibId AND RightBibId = @RightBibId)
                 AND (CASE WHEN ActionId = 1 THEN RightBibId ELSE LeftBibId END) = @KeepBibId",
            new
            {
                UserEmail = userId,
                decision.LeftBibId,
                decision.RightBibId,
                KeepBibId = current.KeepBibId
            });

        if (keepConflict is not null)
        {
            var otherKeepBibId = keepConflict.ActionId == (int)BibDupePairAction.KeepLeft
                ? keepConflict.LeftBibId
                : keepConflict.RightBibId;
            throw new ConflictingMergeDecisionException(
                $"Cannot keep bib {current.KeepBibId} because it is already marked to be merged into bib {otherKeepBibId}.");
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
}
