using System.Data;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionStore(IDbConnection db) : IDecisionStore
{
    private readonly IDbConnection _db = db;
    private const string Table = "DecisionQueue";

    public async Task AddAsync(string userId, DecisionItem decision)
    {
        var parameters = new
        {
            UserEmail = userId,
            decision.LeftBibId,
            decision.RightBibId,
            Action = (int)decision.Action
        };

        var updated = await _db.ExecuteAsync($"UPDATE {Table} SET Action = @Action WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId", parameters);

        if (updated == 0)
        {
            await _db.ExecuteAsync($"INSERT INTO {Table}(UserEmail, LeftBibId, RightBibId, Action) VALUES (@UserEmail, @LeftBibId, @RightBibId, @Action)", parameters);
        }
    }

    public async Task<IEnumerable<DecisionItem>> GetAllAsync(string userId) =>
        await _db.QueryAsync<DecisionItem>(
            $@"SELECT d.LeftBibId, d.RightBibId, d.Action, p.MatchType, p.MatchValue
               FROM {Table} d
               JOIN vwBibDupePairs p ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
               WHERE d.UserEmail = @UserEmail",
            new { UserEmail = userId });

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId) =>
        _db.ExecuteAsync($"DELETE FROM {Table} WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public async Task<int> CountAsync(string userId) =>
        await _db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });
}
