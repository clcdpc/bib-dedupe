using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Dapper;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionBatchHistoryService(IDbConnectionFactory factory) : IDecisionBatchHistoryService
{
    public async Task<IReadOnlyList<DecisionBatchHistory>> GetHistoryAsync(string userEmail)
    {
        using var connection = factory.Create();
        var rows = (await connection.QueryAsync<BatchResultRow>(
            @"SELECT b.BatchId, b.StartedAt, b.CompletedAt, b.FailedAt, b.FailureMessage,
                     r.ResultId, r.LeftBibId, r.RightBibId, r.ActionId, r.Succeeded, r.ErrorMessage, r.ProcessedAt
              FROM BibDedupe.DecisionBatches b
              LEFT JOIN BibDedupe.DecisionBatchResults r ON b.BatchId = r.BatchId
              WHERE b.UserEmail = @UserEmail
              ORDER BY b.StartedAt DESC, r.ProcessedAt, r.ResultId",
            new { UserEmail = userEmail })).ToList();

        if (rows.Count == 0)
        {
            return Array.Empty<DecisionBatchHistory>();
        }

        var batchLookup = new Dictionary<int, BatchAccumulator>();

        foreach (var row in rows)
        {
            if (!batchLookup.TryGetValue(row.BatchId, out var accumulator))
            {
                accumulator = new BatchAccumulator
                {
                    BatchId = row.BatchId,
                    StartedAt = new DateTimeOffset(DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc)),
                    CompletedAt = row.CompletedAt.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAt.Value, DateTimeKind.Utc))
                        : (DateTimeOffset?)null,
                    FailedAt = row.FailedAt.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(row.FailedAt.Value, DateTimeKind.Utc))
                        : (DateTimeOffset?)null,
                    FailureMessage = row.FailureMessage,
                    Results = new List<DecisionBatchResult>()
                };

                batchLookup.Add(row.BatchId, accumulator);
            }

            if (!accumulator.FailedAt.HasValue && row.FailedAt.HasValue)
            {
                accumulator.FailedAt = new DateTimeOffset(DateTime.SpecifyKind(row.FailedAt.Value, DateTimeKind.Utc));
            }

            if (string.IsNullOrWhiteSpace(accumulator.FailureMessage) && !string.IsNullOrWhiteSpace(row.FailureMessage))
            {
                accumulator.FailureMessage = row.FailureMessage;
            }

            if (row.ResultId.HasValue && row.ActionId.HasValue && row.LeftBibId.HasValue && row.RightBibId.HasValue && row.ProcessedAt.HasValue)
            {
                if (!Enum.IsDefined(typeof(BibDupePairAction), row.ActionId.Value))
                {
                    continue;
                }

                accumulator.Results.Add(new DecisionBatchResult
                {
                    LeftBibId = row.LeftBibId.Value,
                    RightBibId = row.RightBibId.Value,
                    Action = (BibDupePairAction)row.ActionId.Value,
                    Succeeded = row.Succeeded ?? false,
                    ErrorMessage = row.ErrorMessage,
                    ProcessedAt = new DateTimeOffset(DateTime.SpecifyKind(row.ProcessedAt.Value, DateTimeKind.Utc))
                });
            }
        }

        var batches = batchLookup.Values
            .OrderByDescending(b => b.StartedAt)
            .Select(b => new DecisionBatchHistory
            {
                BatchId = b.BatchId,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                FailedAt = b.FailedAt,
                FailureMessage = b.FailureMessage,
                Results = b.Results
                    .OrderBy(r => r.ProcessedAt)
                    .ToList()
            })
            .ToList();

        return batches;
    }

    private sealed class BatchAccumulator
    {
        public required int BatchId { get; init; }
        public required DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public DateTimeOffset? FailedAt { get; set; }
        public string? FailureMessage { get; set; }
        public required List<DecisionBatchResult> Results { get; init; }
    }

    private sealed class BatchResultRow
    {
        public int BatchId { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime? FailedAt { get; init; }
        public string? FailureMessage { get; init; }
        public int? ResultId { get; init; }
        public int? LeftBibId { get; init; }
        public int? RightBibId { get; init; }
        public int? ActionId { get; init; }
        public bool? Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime? ProcessedAt { get; init; }
    }
}
