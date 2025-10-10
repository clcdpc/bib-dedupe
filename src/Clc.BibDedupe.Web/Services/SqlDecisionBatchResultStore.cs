using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionBatchResultStore(IDbConnectionFactory factory) : IDecisionBatchResultStore
{
    private const string BatchTable = "BibDedupe.DecisionBatches";
    private const string ResultTable = "BibDedupe.DecisionBatchResults";

    public async Task<IReadOnlyList<DecisionBatchSummary>> GetSummariesAsync(string userEmail)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<SummaryRow>(
            $@"SELECT b.BatchId, b.JobId, b.StartedAt, b.CompletedAt,
                       TotalCount = COUNT(r.ResultId),
                       SuccessCount = COALESCE(SUM(CASE WHEN r.Succeeded = 1 THEN 1 ELSE 0 END), 0),
                       FailureCount = COALESCE(SUM(CASE WHEN r.Succeeded = 0 THEN 1 ELSE 0 END), 0)
                FROM {BatchTable} b
                LEFT JOIN {ResultTable} r ON r.BatchId = b.BatchId
                WHERE b.UserEmail = @UserEmail
                GROUP BY b.BatchId, b.JobId, b.StartedAt, b.CompletedAt
                ORDER BY b.StartedAt DESC",
            new { UserEmail = userEmail });

        return rows.Select(MapSummary).ToList();
    }

    public async Task<DecisionBatchDetail?> GetDetailAsync(string userEmail, int batchId)
    {
        using var connection = factory.Create();
        var summaryRow = await connection.QueryFirstOrDefaultAsync<SummaryRow>(
            $@"SELECT b.BatchId, b.JobId, b.StartedAt, b.CompletedAt,
                       TotalCount = COUNT(r.ResultId),
                       SuccessCount = COALESCE(SUM(CASE WHEN r.Succeeded = 1 THEN 1 ELSE 0 END), 0),
                       FailureCount = COALESCE(SUM(CASE WHEN r.Succeeded = 0 THEN 1 ELSE 0 END), 0)
                FROM {BatchTable} b
                LEFT JOIN {ResultTable} r ON r.BatchId = b.BatchId
                WHERE b.BatchId = @BatchId AND b.UserEmail = @UserEmail
                GROUP BY b.BatchId, b.JobId, b.StartedAt, b.CompletedAt",
            new { BatchId = batchId, UserEmail = userEmail });

        if (summaryRow is null)
        {
            return null;
        }

        var results = await connection.QueryAsync<ResultRow>(
            $@"SELECT r.LeftBibId, r.RightBibId, r.ActionId, r.ProcessedAt, r.Succeeded, r.ErrorMessage
                FROM {ResultTable} r
                INNER JOIN {BatchTable} b ON b.BatchId = r.BatchId
                WHERE r.BatchId = @BatchId AND b.UserEmail = @UserEmail
                ORDER BY r.ProcessedAt ASC, r.LeftBibId, r.RightBibId",
            new { BatchId = batchId, UserEmail = userEmail });

        return new DecisionBatchDetail
        {
            Summary = MapSummary(summaryRow),
            Results = results.Select(MapResult).ToList()
        };
    }

    private static DecisionBatchSummary MapSummary(SummaryRow row) => new()
    {
        BatchId = row.BatchId,
        JobId = row.JobId,
        StartedAt = new DateTimeOffset(DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc)),
        CompletedAt = row.CompletedAt is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc)),
        TotalCount = row.TotalCount,
        SuccessCount = row.SuccessCount,
        FailureCount = row.FailureCount
    };

    private static DecisionBatchResult MapResult(ResultRow row) => new()
    {
        LeftBibId = row.LeftBibId,
        RightBibId = row.RightBibId,
        Action = (BibDupePairAction)row.ActionId,
        ProcessedAt = new DateTimeOffset(DateTime.SpecifyKind(row.ProcessedAt, DateTimeKind.Utc)),
        Succeeded = row.Succeeded,
        ErrorMessage = row.ErrorMessage
    };

    private sealed class SummaryRow
    {
        public int BatchId { get; init; }
        public string JobId { get; init; } = string.Empty;
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public int TotalCount { get; init; }
        public int SuccessCount { get; init; }
        public int FailureCount { get; init; }
    }

    private sealed class ResultRow
    {
        public int LeftBibId { get; init; }
        public int RightBibId { get; init; }
        public int ActionId { get; init; }
        public DateTime ProcessedAt { get; init; }
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
