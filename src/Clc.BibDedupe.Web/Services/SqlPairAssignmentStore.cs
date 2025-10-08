using System;
using System.Data;
using Dapper;

namespace Clc.BibDedupe.Web.Services;

public class SqlPairAssignmentStore(IDbConnection db) : IPairAssignmentStore
{
    private const string Table = "BibDedupe.PairAssignments";

    public Task AssignAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync(
            $@"MERGE {Table} WITH (HOLDLOCK) AS target
USING (SELECT @LeftBibId AS LeftBibId, @RightBibId AS RightBibId) AS source
    ON target.LeftBibId = source.LeftBibId AND target.RightBibId = source.RightBibId
WHEN MATCHED AND target.UserEmail = @UserEmail THEN
    UPDATE SET AssignedAt = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
    INSERT (LeftBibId, RightBibId, UserEmail, AssignedAt)
    VALUES (@LeftBibId, @RightBibId, @UserEmail, SYSDATETIMEOFFSET());",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task ReleaseAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync(
            $"DELETE FROM {Table} WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId AND UserEmail = @UserEmail",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task<int> ReleaseExpiredAsync(DateTimeOffset olderThan) =>
        db.ExecuteAsync(
            $"DELETE FROM {Table} WHERE AssignedAt < @Cutoff",
            new { Cutoff = olderThan });
}
