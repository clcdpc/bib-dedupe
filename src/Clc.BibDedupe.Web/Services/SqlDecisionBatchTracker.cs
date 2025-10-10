using System.Data;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionBatchTracker(IDbConnectionFactory factory) : IDecisionBatchTracker
{
    private const string Table = "BibDedupe.DecisionBatches";

    public async Task CompleteAsync(string userEmail, DateTimeOffset completedAt)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync($"UPDATE {Table} SET CompletedAt = @CompletedAt WHERE UserEmail = @UserEmail AND CompletedAt IS NULL", new
        {
            UserEmail = userEmail,
            CompletedAt = completedAt.UtcDateTime
        });
    }

    public async Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail)
    {
        using var connection = factory.Create();
        var row = await connection.QueryFirstOrDefaultAsync<DecisionBatchRow>($"SELECT TOP 1 BatchId, JobId, StartedAt, CompletedAt FROM {Table} WHERE UserEmail = @UserEmail ORDER BY StartedAt DESC", new { UserEmail = userEmail });

        if (row is null || row.CompletedAt.HasValue)
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
                : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc))
        };
    }

    public async Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        using var connection = factory.Create();
        var batchId = await connection.ExecuteScalarAsync<int>($"INSERT INTO {Table} (UserEmail, JobId, StartedAt) OUTPUT INSERTED.BatchId VALUES (@UserEmail, @JobId, @StartedAt)", new
        {
            UserEmail = userEmail,
            JobId = jobId,
            StartedAt = startedAt.UtcDateTime
        });

        return new DecisionBatchStatus
        {
            BatchId = batchId,
            JobId = jobId,
            StartedAt = startedAt,
            CompletedAt = null
        };
    }

    private sealed class DecisionBatchRow
    {
        public int BatchId { get; init; }
        public string JobId { get; init; } = string.Empty;
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
}
