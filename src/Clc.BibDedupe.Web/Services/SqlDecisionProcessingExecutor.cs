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
        var parameters = new DynamicParameters();
        parameters.Add("UserEmail", userEmail);
        parameters.Add("TotalDecisions", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("SucceededCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("FailedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await db.ExecuteAsync(
            "BibDedupe.ProcessDecisionBatch",
            parameters,
            commandType: CommandType.StoredProcedure,
            commandTimeout: 0);

        return new DecisionProcessingSummary
        {
            TotalDecisions = parameters.Get<int>("TotalDecisions"),
            SucceededCount = parameters.Get<int>("SucceededCount"),
            FailedCount = parameters.Get<int>("FailedCount")
        };
    }
}
