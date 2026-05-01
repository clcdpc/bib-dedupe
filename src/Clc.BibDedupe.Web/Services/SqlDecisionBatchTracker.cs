using System.Data;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionBatchTracker(IDbConnection db) : IDecisionBatchTracker
{
    private const string Table = "BibDedupe.DecisionBatches";

    public async Task CompleteAsync(string userEmail, DateTimeOffset completedAt)
    {
        await db.ExecuteAsync($"UPDATE {Table} SET CompletedAt = @CompletedAt, FailedAt = NULL, FailureMessage = NULL WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL", new
        {
            UserEmail = userEmail,
            CompletedAt = completedAt.UtcDateTime
        });
    }

    public async Task FailAsync(string userEmail, DateTimeOffset failedAt, string errorMessage)
    {
        await db.ExecuteAsync($"UPDATE {Table} SET FailedAt = @FailedAt, FailureMessage = @FailureMessage WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL", new
        {
            UserEmail = userEmail,
            FailedAt = failedAt.UtcDateTime,
            FailureMessage = errorMessage
        });
    }

    public async Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail)
    {
        var row = await db.QueryFirstOrDefaultAsync<DecisionBatchRow>($"SELECT TOP 1 JobId, StartedAt, CompletedAt, FailedAt, FailureMessage FROM {Table} WHERE UserEmail = @UserEmail ORDER BY StartedAt DESC", new { UserEmail = userEmail });

        if (row is null || row.CompletedAt.HasValue || row.FailedAt.HasValue)
        {
            return null;
        }

        return new DecisionBatchStatus
        {
            JobId = row.JobId,
            StartedAt = new DateTimeOffset(DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc)),
            CompletedAt = row.CompletedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc)),
            FailedAt = row.FailedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.FailedAt.Value, DateTimeKind.Utc)),
            FailureMessage = row.FailureMessage
        };
    }

    public async Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt)
    {
        await db.ExecuteAsync($"INSERT INTO {Table} (UserEmail, JobId, StartedAt) VALUES (@UserEmail, @JobId, @StartedAt)", new
        {
            UserEmail = userEmail,
            JobId = string.Empty,
            StartedAt = startedAt.UtcDateTime
        });

        return new DecisionBatchStatus { JobId = string.Empty, StartedAt = startedAt };
    }

    public async Task<DecisionBatchStatus> SetJobIdAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        var updatedRows = await db.ExecuteAsync($"UPDATE {Table} SET JobId = @JobId WHERE UserEmail = @UserEmail AND StartedAt = @StartedAt AND CompletedAt IS NULL AND FailedAt IS NULL", new
        {
            UserEmail = userEmail,
            StartedAt = startedAt.UtcDateTime,
            JobId = jobId
        });

        if (updatedRows == 0)
        {
            throw new InvalidOperationException($"No active decision batch found for {userEmail} at {startedAt:o}.");
        }

        return new DecisionBatchStatus { JobId = jobId, StartedAt = startedAt };
    }

    private sealed class DecisionBatchRow
    {
        public string JobId { get; init; } = string.Empty;
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime? FailedAt { get; init; }
        public string? FailureMessage { get; init; }
    }
}
