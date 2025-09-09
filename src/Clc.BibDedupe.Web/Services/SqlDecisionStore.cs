using System.Data;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionStore(IDbConnection db) : IDecisionStore
{
    private readonly IDbConnection _db = db;
    private const string Table = "DecisionQueue";

    public Task AddAsync(string userId, DecisionItem decision) =>
        _db.ExecuteAsync($"INSERT INTO {Table}(UserEmail, LeftBibId, RightBibId, Action) VALUES (@UserEmail, @LeftBibId, @RightBibId, @Action)",
            new { UserEmail = userId, decision.LeftBibId, decision.RightBibId, Action = (int)decision.Action });

    public async Task<IEnumerable<DecisionItem>> GetAllAsync(string userId) =>
        await _db.QueryAsync<DecisionItem>($"SELECT LeftBibId, RightBibId, Action FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });

    public Task RemoveAsync(string userId, int leftBibId, int rightBibId) =>
        _db.ExecuteAsync($"DELETE FROM {Table} WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task UpdateAsync(string userId, DecisionItem decision) =>
        _db.ExecuteAsync($"UPDATE {Table} SET Action = @Action WHERE UserEmail = @UserEmail AND LeftBibId = @LeftBibId AND RightBibId = @RightBibId",
            new { UserEmail = userId, decision.LeftBibId, decision.RightBibId, Action = (int)decision.Action });

    public async Task<int> CountAsync(string userId) =>
        await _db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Table} WHERE UserEmail = @UserEmail", new { UserEmail = userId });
}
