using System.Data;
using System.Threading;
using Dapper;

namespace Clc.BibDedupe.Web.Services;

public class SqlDecisionProcessingExecutor(IDecisionProcessingDbConnectionFactory factory)
    : IDecisionProcessingExecutor
{
    public Task<bool> CanProcessAsync() => Task.FromResult(true);

    public async Task ExecuteAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync(
            "clcdb.BibDedupe.ProcessDecisionBatch",
            new { UserEmail = userEmail },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 0);
    }
}
