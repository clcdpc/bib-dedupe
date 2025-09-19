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
                      p.MatchesJson
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

    private static DecisionItem MapRow(DecisionRow row) => new()
    {
        LeftBibId = row.LeftBibId,
        RightBibId = row.RightBibId,
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
        public string MatchesJson { get; init; } = string.Empty;
    }
}
