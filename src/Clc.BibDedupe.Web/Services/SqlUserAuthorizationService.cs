using System;
using System.Collections.Generic;
using System.Linq;
using Clc.BibDedupe.Web.Authorization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clc.BibDedupe.Web.Services;

public class SqlUserAuthorizationService(IConfiguration config) : IUserAuthorizationService
{
    private readonly string? _connectionString =
        config.GetConnectionString("AuthorizedUsersDb") ?? config.GetConnectionString("BibDedupeDb");

    private const string Query =
        "SELECT Claim FROM BibDedupe.UserClaims WHERE UserEmail = @Email";

    public async Task<bool> IsAuthorizedAsync(string email)
    {
        var claims = await GetClaimsAsync(email);
        return claims.Contains(UserRoles.Access);
    }

    public async Task<IReadOnlyCollection<string>> GetClaimsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(email))
        {
            return Array.Empty<string>();
        }

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<UserClaimRow>(Query, new { Email = email });

        return rows
            .Select(row => row.Claim)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record UserClaimRow(string? Claim);
}
