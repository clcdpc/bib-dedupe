using System.Data;
using System.Threading;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionProcessingExecutor(IDbConnection db) : IDecisionProcessingExecutor
{
    public Task<bool> CanProcessAsync() => Task.FromResult(true);

    public async Task<DecisionProcessingSummary> ExecuteAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        await db.ExecuteAsync(
            "BibDedupe.ProcessDecisionBatch",
            new { UserEmail = userEmail },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 0);

        var summary = await db.QueryFirstOrDefaultAsync<SummaryRow>(
            @"SELECT TOP 1
                    COUNT(*) AS TotalDecisions,
                    SUM(CASE WHEN r.Succeeded = 1 THEN 1 ELSE 0 END) AS SucceededCount,
                    SUM(CASE WHEN r.Succeeded = 0 THEN 1 ELSE 0 END) AS FailedCount
              FROM BibDedupe.DecisionBatches b
              INNER JOIN BibDedupe.DecisionBatchResults r ON r.BatchId = b.BatchId
              WHERE b.UserEmail = @UserEmail
              GROUP BY b.BatchId
              ORDER BY b.StartedAt DESC",
            new { UserEmail = userEmail });

        return new DecisionProcessingSummary
        {
            TotalDecisions = summary?.TotalDecisions ?? 0,
            SucceededCount = summary?.SucceededCount ?? 0,
            FailedCount = summary?.FailedCount ?? 0
        };
    }

    private sealed class SummaryRow
    {
        public int TotalDecisions { get; init; }
        public int SucceededCount { get; init; }
        public int FailedCount { get; init; }
    }
}
