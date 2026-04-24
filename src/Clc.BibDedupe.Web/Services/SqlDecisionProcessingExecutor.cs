using System.Data;
using System.Threading;
using Dapper;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionProcessingExecutor(IDbConnection db)
    : IDecisionProcessingExecutor
{
    public Task<bool> CanProcessAsync() => Task.FromResult(true);

    public async Task ExecuteAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        await db.ExecuteAsync(
            "BibDedupe.ProcessDecisionBatch",
            new { UserEmail = userEmail },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 0);
    }
}
