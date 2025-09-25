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

        private const string PagedPairsCte = @"WITH PairSource AS (
    SELECT
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
    FROM BibDedupe.GetPairs(@Top)
),
OrderedPairs AS (
    SELECT
        ps.*,
        ROW_NUMBER() OVER (PARTITION BY ps.PrimaryMarcTomId ORDER BY ps.PairId) AS RowNumberInGroup,
        COUNT(*) OVER (PARTITION BY ps.PrimaryMarcTomId) AS GroupSize,
        MIN(ps.PairId) OVER (PARTITION BY ps.PrimaryMarcTomId) AS FirstPairId
    FROM PairSource ps
),
ChunkedPairs AS (
    SELECT
        op.*,
        (op.RowNumberInGroup - 1) / @PageSize AS ChunkIndex
    FROM OrderedPairs op
),
GroupChunks AS (
    SELECT
        cp.PrimaryMarcTomId,
        cp.ChunkIndex,
        MIN(cp.FirstPairId) AS FirstPairId,
        COUNT(*) AS ChunkSize
    FROM ChunkedPairs cp
    GROUP BY cp.PrimaryMarcTomId, cp.ChunkIndex
),
OrderedChunks AS (
    SELECT
        gc.*,
        ROW_NUMBER() OVER (ORDER BY gc.FirstPairId, gc.PrimaryMarcTomId, gc.ChunkIndex) AS ChunkOrder,
        SUM(gc.ChunkSize) OVER (ORDER BY gc.FirstPairId, gc.PrimaryMarcTomId, gc.ChunkIndex ROWS UNBOUNDED PRECEDING) AS RunningTotal
    FROM GroupChunks gc
),
PagedChunks AS (
    SELECT
        oc.*,
        ((oc.RunningTotal - 1) / @PageSize) + 1 AS PageNumber
    FROM OrderedChunks oc
),
PagedPairs AS (
    SELECT
        cp.PairId,
        cp.PrimaryMarcTomId,
        cp.LeftBibId,
        cp.RightBibId,
        cp.LeftTitle,
        cp.LeftAuthor,
        cp.RightTitle,
        cp.RightAuthor,
        cp.MatchesJson,
        cp.LeftHoldCount,
        cp.RightHoldCount,
        cp.TotalHoldCount,
        cp.FirstPairId,
        cp.RowNumberInGroup,
        pc.PageNumber
    FROM ChunkedPairs cp
    INNER JOIN PagedChunks pc
        ON cp.PrimaryMarcTomId = pc.PrimaryMarcTomId
       AND cp.ChunkIndex = pc.ChunkIndex
)";

        private const string PagedPairsStatsSql = PagedPairsCte + @"
SELECT
    COUNT(*) AS TotalCount,
    ISNULL(MAX(PageNumber), 0) AS TotalPages
FROM PagedPairs;";

        private const string PagedPairsDataSql = PagedPairsCte + @"
SELECT
    PairId,
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
FROM PagedPairs
WHERE PageNumber = @TargetPage
ORDER BY FirstPairId, PrimaryMarcTomId, RowNumberInGroup;";

        public BibDupePairRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BibDupePair>> GetAsync()
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson,
       LeftHoldCount, RightHoldCount, TotalHoldCount
FROM BibDedupe.GetPairs(@Top)";
            var rows = await _db.QueryAsync<PairRow>(sql, new { Top = UnlimitedPairsLimit });
            return rows.Select(MapRow).ToList();
        }

        public async Task<(IEnumerable<BibDupePair> Items, int TotalCount, int TotalPages)> GetPagedAsync(int page, int pageSize)
        {
            var normalizedPageSize = Math.Max(pageSize, 1);
            var requestedPage = Math.Max(page, 1);

            var stats = await _db.QuerySingleAsync<PaginationStats>(
                PagedPairsStatsSql,
                new { Top = UnlimitedPairsLimit, PageSize = normalizedPageSize });

            var clampedPage = stats.TotalPages <= 0 ? 1 : Math.Min(requestedPage, stats.TotalPages);

            var rows = await _db.QueryAsync<PairRow>(
                PagedPairsDataSql,
                new { Top = UnlimitedPairsLimit, PageSize = normalizedPageSize, TargetPage = clampedPage });

            var items = rows.Select(MapRow).ToList();
            return (items, stats.TotalCount, stats.TotalPages);
        }

        public async Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson,
       LeftHoldCount, RightHoldCount, TotalHoldCount
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

        private sealed class PaginationStats
        {
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
        }
    }
}
