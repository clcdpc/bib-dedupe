using System;
using System.Data;
using Dapper;

namespace Clc.BibDedupe.Web.Services;

public class SqlPairAssignmentStore(IDbConnection db) : IPairAssignmentStore
{
    public Task AssignAsync(string userId, int leftBibId, int rightBibId) =>
        db.ExecuteAsync(
            @"MERGE BibDedupe.PairAssignments WITH (HOLDLOCK) AS target
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
            "DELETE FROM BibDedupe.PairAssignments WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId AND UserEmail = @UserEmail",
            new { UserEmail = userId, LeftBibId = leftBibId, RightBibId = rightBibId });

    public Task<int> ReleaseExpiredAsync(DateTimeOffset olderThan) =>
        db.ExecuteAsync(
            "DELETE FROM BibDedupe.PairAssignments WHERE AssignedAt < @Cutoff",
            new { Cutoff = olderThan });
}
