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
            const string sql = @"DECLARE @NormalizedPageSize INT = CASE WHEN @PageSize <= 0 THEN 1 ELSE @PageSize END;
DECLARE @NormalizedPage INT = CASE WHEN @Page <= 0 THEN 1 ELSE @Page END;

;WITH PairSource AS (
    SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
           LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
    FROM BibDedupe.GetPairs(DEFAULT)
),
GroupCounts AS (
    SELECT PrimaryMarcTomId,
           COUNT(*) AS PairCount
    FROM PairSource
    GROUP BY PrimaryMarcTomId
),
OrderedGroups AS (
    SELECT PrimaryMarcTomId,
           PairCount,
           SUM(PairCount) OVER (ORDER BY PrimaryMarcTomId ROWS UNBOUNDED PRECEDING) AS RunningTotal
    FROM GroupCounts
),
GroupPages AS (
    SELECT PrimaryMarcTomId,
           PairCount,
           RunningTotal,
           RunningTotal - PairCount AS RunningTotalBefore,
           (RunningTotal - PairCount) / @NormalizedPageSize + 1 AS PageNumber
    FROM OrderedGroups
)
SELECT ps.PairId, ps.PrimaryMarcTomId, ps.LeftBibId, ps.RightBibId,
       ps.LeftTitle, ps.LeftAuthor, ps.RightTitle, ps.RightAuthor, ps.MatchesJson
FROM PairSource ps
JOIN GroupPages gp ON gp.PrimaryMarcTomId = ps.PrimaryMarcTomId
WHERE gp.PageNumber = @NormalizedPage
ORDER BY ps.PrimaryMarcTomId, ps.PairId;

SELECT COUNT(*) FROM BibDedupe.GetPairs(DEFAULT);

;WITH PairSource AS (
    SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId
    FROM BibDedupe.GetPairs(DEFAULT)
),
GroupCounts AS (
    SELECT PrimaryMarcTomId,
           COUNT(*) AS PairCount
    FROM PairSource
    GROUP BY PrimaryMarcTomId
),
OrderedGroups AS (
    SELECT PrimaryMarcTomId,
           PairCount,
           SUM(PairCount) OVER (ORDER BY PrimaryMarcTomId ROWS UNBOUNDED PRECEDING) AS RunningTotal
    FROM GroupCounts
),
GroupPages AS (
    SELECT (RunningTotal - PairCount) / @NormalizedPageSize + 1 AS PageNumber
    FROM OrderedGroups
)
SELECT COALESCE(MAX(PageNumber), 0) FROM GroupPages;";

            var parameters = new { Page = page, PageSize = pageSize };
            using var multi = await _db.QueryMultipleAsync(sql, parameters);
            var rows = await multi.ReadAsync<PairRow>();
            var total = await multi.ReadFirstAsync<int>();
            var totalPages = await multi.ReadFirstAsync<int>();
            var items = rows.Select(MapRow).ToList();
            return (items, total, totalPages);
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
