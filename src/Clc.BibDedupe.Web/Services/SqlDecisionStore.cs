using System.Data;
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

    public async Task<IEnumerable<DecisionItem>> GetAllAsync(string userId) =>
            await db.QueryAsync<DecisionItem>(
                $@"SELECT d.LeftBibId, d.RightBibId, d.ActionId AS Action, p.MatchType, p.MatchValue, p.PrimaryMARCTOMID AS PrimaryMarcTomId,
                      p.LeftTitle, p.LeftAuthor, p.RightTitle, p.RightAuthor
               FROM {Table} d
               JOIN BibDedupe.GetPairs(DEFAULT) p ON d.LeftBibId = p.LeftBibId AND d.RightBibId = p.RightBibId
               WHERE d.UserEmail = @UserEmail",
                new { UserEmail = userId });

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync($"DELETE FROM {Table} WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task UpdateAsync(string userId, DecisionItem decision) => AddAsync(userId, decision);

    public async Task<int> CountAsync(string userId) =>
        await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });
}
