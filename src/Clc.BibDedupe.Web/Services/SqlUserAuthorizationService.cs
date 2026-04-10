using System;
using System.Collections.Generic;
using Clc.BibDedupe.Web.Authorization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clc.BibDedupe.Web.Services;

public class SqlUserAuthorizationService(IConfiguration config) : IUserAuthorizationService
{
    private readonly string? _connectionString = config.GetConnectionString("BibDedupeDb");

    private const string Query =
        "SELECT Claim FROM BibDedupe.UserClaims WHERE UserEmail = @Email";

    public async Task<bool> IsAuthorizedAsync(string email)
    {
        var claims = await GetClaimsAsync(email);
        return claims.Contains(UserRoles.Access) || claims.Contains(UserRoles.Administrator);
    }

    public async Task<IReadOnlyCollection<string>> GetClaimsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(email))
        {
            return Array.Empty<string>();
        }

        await using var conn = new SqlConnection(_connectionString);
        var claims = await conn.QueryAsync<string>(Query, new { Email = email });
        return claims.AsList();
    }

}
