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
        await connection.ExecuteAsync($"UPDATE {Table} SET CompletedAt = @CompletedAt, FailedAt = NULL, FailureMessage = NULL WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL", new
        {
            UserEmail = userEmail,
            CompletedAt = completedAt.UtcDateTime
        });
    }

    public async Task FailAsync(string userEmail, DateTimeOffset failedAt, string errorMessage)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync($"UPDATE {Table} SET FailedAt = @FailedAt, FailureMessage = @FailureMessage WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL", new
        {
            UserEmail = userEmail,
            FailedAt = failedAt.UtcDateTime,
            FailureMessage = errorMessage
        });
    }

    public async Task<DecisionBatchStatus?> GetCurrentAsync(string userEmail)
    {
        using var connection = factory.Create();
        var row = await connection.QueryFirstOrDefaultAsync<DecisionBatchRow>($"SELECT TOP 1 JobId, StartedAt, CompletedAt, FailedAt, FailureMessage FROM {Table} WHERE UserEmail = @UserEmail ORDER BY StartedAt DESC", new { UserEmail = userEmail });

        if (row is null || row.CompletedAt.HasValue || row.FailedAt.HasValue)
        {
            return null;
        }

        return new DecisionBatchStatus
        {
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

    public async Task<DecisionBatchStatus> StartAsync(string userEmail, DateTimeOffset startedAt, string jobId)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync($"INSERT INTO {Table} (UserEmail, JobId, StartedAt) VALUES (@UserEmail, @JobId, @StartedAt)", new
        {
            UserEmail = userEmail,
            JobId = jobId,
            StartedAt = startedAt.UtcDateTime
        });

        return new DecisionBatchStatus
        {
            JobId = jobId,
            StartedAt = startedAt,
            CompletedAt = null,
            FailedAt = null,
            FailureMessage = null
        };
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
