using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data
{
    public class BibDupePairRepository : IBibDupePairRepository
    {
        private readonly IDbConnection _db;
        private const int UnlimitedPairsLimit = int.MaxValue;

        public BibDupePairRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BibDupePair>> GetAsync()
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
FROM BibDedupe.GetPairs(DEFAULT)";
            var rows = await _db.QueryAsync<PairRow>(sql);
            return rows.Select(MapRow).ToList();
        }

        public async Task<(IEnumerable<BibDupePair> Items, int TotalCount, int TotalPages)> GetPagedAsync(int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            const string commonTableExpression = @"WITH OrderedPairs AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum,
        ROW_NUMBER() OVER (PARTITION BY LeftBibId ORDER BY (SELECT NULL)) AS RowInGroup,
        PairId,
        PrimaryMARCTOMID AS PrimaryMarcTomId,
        LeftBibId,
        RightBibId,
        LeftTitle,
        LeftAuthor,
        RightTitle,
        RightAuthor,
        MatchesJson,
        LeftHoldCount,
        RightHoldCount,
        TotalHoldCount
    FROM BibDedupe.GetPairs(DEFAULT)
), GroupedPairs AS (
    SELECT *,
           RowNum - RowInGroup + 1 AS GroupStartRow
    FROM OrderedPairs
), PageAssignments AS (
    SELECT *,
           CAST(((GroupStartRow - 1) / CAST(@PageSize AS BIGINT)) + 1 AS INT) AS PageNumber
    FROM GroupedPairs
)";

            var sql = $@"{commonTableExpression}
SELECT PairId,
       PrimaryMarcTomId,
       LeftBibId,
       RightBibId,
       LeftTitle,
       LeftAuthor,
       RightTitle,
       RightAuthor,
       MatchesJson,
       LeftHoldCount,
       RightHoldCount,
       TotalHoldCount
FROM PageAssignments
WHERE PageNumber = @Page
ORDER BY RowNum;
{commonTableExpression}
SELECT COUNT(*) AS TotalCount,
       COALESCE(MAX(PageNumber), 0) AS TotalPages
FROM PageAssignments;";

            using var multi = await _db.QueryMultipleAsync(sql, new { Page = page, PageSize = pageSize });
            var rows = await multi.ReadAsync<PairRow>();
            var summary = await multi.ReadFirstAsync<PageSummaryRow>();
            var items = rows.Select(MapRow).ToList();
            return (items, summary.TotalCount, summary.TotalPages);
        }

        public async Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
FROM BibDedupe.GetPairs(@Top)
WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;";
            var row = await _db.QueryFirstOrDefaultAsync<PairRow>(sql, new { LeftBibId = leftBibId, RightBibId = rightBibId, Top = UnlimitedPairsLimit });
            return row is null ? null : MapRow(row);
        }

        public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) =>
            _db.ExecuteAsync(
                "BibDedupe.MergePair",
                new { KeepBibId = keepBibId, DeleteBibId = deleteBibId, UserEmail = userEmail, ActionId = (int)action },
                commandType: CommandType.StoredProcedure);

        public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) =>
            _db.ExecuteAsync(
                "BibDedupe.KeepBoth",
                new { LeftBibId = leftBibId, RightBibId = rightBibId, UserEmail = userEmail },
                commandType: CommandType.StoredProcedure);

        public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) =>
            _db.ExecuteAsync(
                "BibDedupe.Skip",
                new { LeftBibId = leftBibId, RightBibId = rightBibId, UserEmail = userEmail },
                commandType: CommandType.StoredProcedure);

        private static BibDupePair MapRow(PairRow row) => new()
        {
            PairId = row.PairId,
            PrimaryMarcTomId = row.PrimaryMarcTomId,
            LeftBibId = row.LeftBibId,
            RightBibId = row.RightBibId,
            LeftTitle = row.LeftTitle,
            LeftAuthor = row.LeftAuthor,
            RightTitle = row.RightTitle,
            RightAuthor = row.RightAuthor,
            LeftHoldCount = row.LeftHoldCount,
            RightHoldCount = row.RightHoldCount,
            TotalHoldCount = row.TotalHoldCount,
            Matches = PairMatch.FromJson(row.MatchesJson)
        };

        private sealed class PageSummaryRow
        {
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
        }

        private sealed class PairRow
        {
            public int PairId { get; set; }
            public int PrimaryMarcTomId { get; set; }
            public int LeftBibId { get; set; }
            public int RightBibId { get; set; }
            public string? LeftTitle { get; set; }
            public string? LeftAuthor { get; set; }
            public string? RightTitle { get; set; }
            public string? RightAuthor { get; set; }
            public int LeftHoldCount { get; set; }
            public int RightHoldCount { get; set; }
            public int TotalHoldCount { get; set; }
            public string MatchesJson { get; set; } = string.Empty;
        }
    }
}
