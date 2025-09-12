using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clc.BibDedupe.Web.Services;

public class SqlUserAuthorizationService(IConfiguration config) : IUserAuthorizationService
{
    private readonly string? _connectionString =
        config.GetConnectionString("AuthorizedUsersDb") ?? config.GetConnectionString("BibDedupeDb");
    private const string Query = "EXEC BibDedupe.IsAuthorizedUser @Email";

    public async Task<bool> IsAuthorizedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return false;
        }

        await using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(Query, new { Email = email }) > 0;
    }
}

