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
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId, LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
FROM BibDedupe.GetPairs(DEFAULT)";
            var rows = await _db.QueryAsync<PairRow>(sql);
            return rows.Select(MapRow).ToList();
        }

        public async Task<(IEnumerable<BibDupePair> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId, LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
FROM BibDedupe.GetPairs(DEFAULT)
ORDER BY (select null)
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
SELECT COUNT(*) FROM BibDedupe.GetPairs(DEFAULT);";
            var offset = (page - 1) * pageSize;
            using var multi = await _db.QueryMultipleAsync(sql, new { Offset = offset, PageSize = pageSize });
            var rows = await multi.ReadAsync<PairRow>();
            var total = await multi.ReadFirstAsync<int>();
            var items = rows.Select(MapRow).ToList();
            return (items, total);
        }

        public async Task<BibDupePair?> GetByBibIdsAsync(int leftBibId, int rightBibId)
        {
            const string sql = @"SELECT PairId, PrimaryMARCTOMID AS PrimaryMarcTomId, LeftBibId, RightBibId, LeftTitle, LeftAuthor, RightTitle, RightAuthor, MatchesJson
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
            LeftTitle = row.LeftTitle ?? string.Empty,
            LeftAuthor = row.LeftAuthor ?? string.Empty,
            RightTitle = row.RightTitle ?? string.Empty,
            RightAuthor = row.RightAuthor ?? string.Empty,
            Matches = PairMatch.FromJson(row.MatchesJson)
        };

        private sealed class PairRow
        {
            public int PairId { get; set; }
            public int PrimaryMarcTomId { get; set; }
            public int LeftBibId { get; set; }
            public int RightBibId { get; set; }
            public string LeftTitle { get; set; } = string.Empty;
            public string LeftAuthor { get; set; } = string.Empty;
            public string RightTitle { get; set; } = string.Empty;
            public string RightAuthor { get; set; } = string.Empty;
            public string MatchesJson { get; set; } = string.Empty;
        }
    }
}
