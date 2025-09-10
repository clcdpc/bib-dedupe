using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Data
{
    public class BibDupePairRepository : IBibDupePairRepository
    {
        private readonly IDbConnection _db;

        public BibDupePairRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BibDupePair>> GetAsync()
        {
            const string sql = "SELECT MatchType, MatchValue, LeftBibId, RightBibId FROM vwBibDupePairs";
            return await _db.QueryAsync<BibDupePair>(sql);
        }

        public async Task<(IEnumerable<BibDupePair> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            const string sql = @"SELECT MatchType, MatchValue, LeftBibId, RightBibId
FROM vwBibDupePairs
ORDER BY LeftBibId, RightBibId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
SELECT COUNT(*) FROM vwBibDupePairs;";
            var offset = (page - 1) * pageSize;
            using var multi = await _db.QueryMultipleAsync(sql, new { Offset = offset, PageSize = pageSize });
            var items = await multi.ReadAsync<BibDupePair>();
            var total = await multi.ReadFirstAsync<int>();
            return (items, total);
        }

        public Task MergeAsync(int keepBibId, int deleteBibId, string userEmail) =>
            _db.ExecuteAsync(
                "MergeBibPair",
                new { KeepBibId = keepBibId, DeleteBibId = deleteBibId, UserEmail = userEmail },
                commandType: CommandType.StoredProcedure);

        public Task KeepBothAsync(int leftBibId, int rightBibId, string userEmail) =>
            _db.ExecuteAsync(
                "BibDupePairs_KeepBoth",
                new { LeftBibId = leftBibId, RightBibId = rightBibId, UserEmail = userEmail },
                commandType: CommandType.StoredProcedure);

        public Task SkipAsync(int leftBibId, int rightBibId, string userEmail) =>
            _db.ExecuteAsync(
                "BibDupePairs_Skip",
                new { LeftBibId = leftBibId, RightBibId = rightBibId, UserEmail = userEmail },
                commandType: CommandType.StoredProcedure);
    }
}
