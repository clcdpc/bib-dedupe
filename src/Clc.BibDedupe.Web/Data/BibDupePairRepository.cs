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

        public async Task<IEnumerable<BibDupePair>> GetAsync(
            string? userEmail = null,
            int? tomId = null,
            string? matchType = null,
            bool? hasHolds = null,
            bool hideDecided = true)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, TOM, LeftHoldCount, RightHoldCount, TotalHoldCount, MatchesJson
FROM BibDedupe.GetPairs(DEFAULT, @UserEmail, @HideDecided, @TomId, @MatchType, @HasHolds)";
            var rows = await _db.QueryAsync<PairRow>(
                sql,
                new
                {
                    UserEmail = userEmail,
                    HideDecided = hideDecided,
                    TomId = tomId,
                    MatchType = matchType,
                    HasHolds = hasHolds
                });
            return rows.Select(MapRow).ToList();
        }

        public async Task<PairsPageResult> GetPagedAsync(
            int page,
            int pageSize,
            string? userEmail = null,
            int? tomId = null,
            string? matchType = null,
            bool? hasHolds = null,
            bool hideDecided = true)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, TOM, LeftHoldCount, RightHoldCount, TotalHoldCount, MatchesJson
FROM BibDedupe.GetPairs(DEFAULT, @UserEmail, @HideDecided, @TomId, @MatchType, @HasHolds) p
ORDER BY p.LeftTitle, p.RightTitle, p.PairId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
SELECT COUNT(*) FROM BibDedupe.GetPairs(@CountTop, @UserEmail, @HideDecided, @TomId, @MatchType, @HasHolds) p;
SELECT DISTINCT gp.PrimaryMarcTomId AS TomId, gp.TOM AS Description
FROM BibDedupe.GetPairs(@CountTop, @UserEmail, @HideDecided, NULL, @MatchType, @HasHolds) gp
WHERE gp.PrimaryMarcTomId IS NOT NULL AND gp.TOM IS NOT NULL AND LTRIM(RTRIM(gp.TOM)) <> ''
ORDER BY gp.TOM;
SELECT DISTINCT pm.MatchType
FROM BibDedupe.GetPairs(@CountTop, @UserEmail, @HideDecided, @TomId, NULL, @HasHolds) gp
JOIN BibDedupe.PairMatches pm ON pm.PairId = gp.PairId
ORDER BY pm.MatchType;";
            var offset = (page - 1) * pageSize;
            using var multi = await _db.QueryMultipleAsync(
                sql,
                new
                {
                    Offset = offset,
                    PageSize = pageSize,
                    CountTop = UnlimitedPairsLimit,
                    UserEmail = userEmail,
                    HideDecided = hideDecided,
                    TomId = tomId,
                    MatchType = matchType,
                    HasHolds = hasHolds
                });
            var rows = await multi.ReadAsync<PairRow>();
            var total = await multi.ReadFirstAsync<int>();
            var tomOptionRows = (await multi.ReadAsync<TomOptionRow>()).ToList();
            var matchTypeOptions = (await multi.ReadAsync<string>()).ToList();
            var items = rows.Select(MapRow).ToList();
            return new PairsPageResult
            {
                Items = items,
                TotalCount = total,
                TomOptions = tomOptionRows
                    .Select(o => new TomOption(o.TomId, o.Description))
                    .ToList(),
                MatchTypeOptions = matchTypeOptions
            };
        }

        public async Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId, string? userEmail = null, bool hideDecided = true)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId,
       LeftTitle, LeftAuthor, RightTitle, RightAuthor, TOM, LeftHoldCount, RightHoldCount, TotalHoldCount, MatchesJson
 FROM BibDedupe.GetPairs(@Top, @UserEmail, @HideDecided, NULL, NULL, NULL)
 WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;";
            var row = await _db.QueryFirstOrDefaultAsync<PairRow>(
                sql,
                new { LeftBibId = leftBibId, RightBibId = rightBibId, Top = UnlimitedPairsLimit, UserEmail = userEmail, HideDecided = hideDecided });
            return row is null ? null : MapRow(row);
        }

        public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail, BibDupePairAction action) =>
            _db.ExecuteAsync(
                "BibDedupe.MergePair",
                new { KeepBibId = keepBibId, DeleteBibId = deleteBibId, UserEmail = userEmail, ActionId = (int)action },
                commandType: CommandType.StoredProcedure);

        public Task MarkNotDuplicateAsync(int leftBibId, int rightBibId, string userEmail) =>
            _db.ExecuteAsync(
                "BibDedupe.MarkNotDuplicate",
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
            TOM = row.TOM,
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
            public string? TOM { get; set; }
            public int LeftHoldCount { get; set; }
            public int RightHoldCount { get; set; }
            public int TotalHoldCount { get; set; }
            public string MatchesJson { get; set; } = string.Empty;
        }

        private sealed class TomOptionRow
        {
            public int TomId { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}
