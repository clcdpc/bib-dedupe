using System.Data;
using Dapper;
using Clc.BibDedupe.Web.Models;
using Microsoft.Data.SqlClient;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionBatchTracker(IDbConnection db) : IDecisionBatchTracker
{
    private const string Table = "BibDedupe.DecisionBatches";
    public Task FailOrphanedPendingAsync(DateTimeOffset staleBefore, string failureMessage) =>
        db.ExecuteAsync(
            $"UPDATE {Table} SET FailedAt = @FailedAt, FailureMessage = @FailureMessage WHERE CompletedAt IS NULL AND FailedAt IS NULL AND (JobId IS NULL OR JobId = '') AND StartedAt <= @StaleBefore",
            new { FailedAt = DateTimeOffset.UtcNow.UtcDateTime, FailureMessage = failureMessage, StaleBefore = staleBefore.UtcDateTime });

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
        var row = await db.QueryFirstOrDefaultAsync<DecisionBatchRow>($"SELECT TOP 1 BatchId, JobId, StartedAt, CompletedAt, FailedAt, FailureMessage FROM {Table} WHERE UserEmail = @UserEmail ORDER BY StartedAt DESC", new { UserEmail = userEmail });

        if (row is null || row.CompletedAt.HasValue || row.FailedAt.HasValue)
        {
            return null;
        }

        return new DecisionBatchStatus
        {
            BatchId = row.BatchId,
            JobId = row.JobId,
            StartedAt = new DateTimeOffset(DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc)),
            CompletedAt = row.CompletedAt is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc)),
            FailedAt = row.FailedAt is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(row.FailedAt.Value, DateTimeKind.Utc)),
            FailureMessage = row.FailureMessage
        };
    }

    public async Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt)
    {
        try
        {
            var batchId = await db.QuerySingleAsync<int>($"INSERT INTO {Table} (UserEmail, JobId, StartedAt) OUTPUT INSERTED.BatchId VALUES (@UserEmail, @JobId, @StartedAt)", new
            {
                UserEmail = userEmail,
                JobId = string.Empty,
                StartedAt = startedAt.UtcDateTime
            });

            return new DecisionBatchStatus
            {
                BatchId = batchId,
                JobId = string.Empty,
                StartedAt = startedAt,
                CompletedAt = null,
                FailedAt = null,
                FailureMessage = null
            };
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ActiveDecisionBatchExistsException(userEmail, ex);
        }
    }

    public async Task<DecisionBatchStatus> SetJobIdAsync(int batchId, string jobId)
    {
        var rows = await db.ExecuteAsync(
            $"UPDATE {Table} SET JobId = @JobId WHERE BatchId = @BatchId AND CompletedAt IS NULL AND FailedAt IS NULL AND (JobId IS NULL OR JobId = '')",
            new { BatchId = batchId, JobId = jobId });

        if (rows == 0)
        {
            rows = await db.ExecuteAsync(
                $"UPDATE {Table} SET JobId = @JobId WHERE BatchId = @BatchId AND (JobId IS NULL OR JobId = '')",
                new { BatchId = batchId, JobId = jobId });
        }

        var row = await db.QueryFirstOrDefaultAsync<DecisionBatchRow>(
            $"SELECT TOP 1 BatchId, JobId, StartedAt, CompletedAt, FailedAt, FailureMessage FROM {Table} WHERE BatchId = @BatchId",
            new { BatchId = batchId });

        if (row is null)
        {
            throw new InvalidOperationException($"Unable to set JobId for batch {batchId}.");
        }

        return new DecisionBatchStatus
        {
            BatchId = row.BatchId,
            JobId = string.IsNullOrWhiteSpace(row.JobId) ? jobId : row.JobId,
            StartedAt = new DateTimeOffset(DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc)),
            CompletedAt = row.CompletedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc)),
            FailedAt = row.FailedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.FailedAt.Value, DateTimeKind.Utc)),
            FailureMessage = row.FailureMessage
        };
    }

    private sealed class DecisionBatchRow
    {
        public int BatchId { get; init; }
        public string JobId { get; init; } = string.Empty;
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime? FailedAt { get; init; }
        public string? FailureMessage { get; init; }
    }
}
