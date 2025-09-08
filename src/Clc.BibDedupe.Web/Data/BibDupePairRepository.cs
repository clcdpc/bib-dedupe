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
