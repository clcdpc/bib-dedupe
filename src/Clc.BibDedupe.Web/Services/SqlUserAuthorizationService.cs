using System;
using System.Collections.Generic;
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
        "SELECT * FROM BibDedupe.UserClaims WHERE UserEmail = @Email";

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
        var rows = await conn.QueryAsync(Query, new { Email = email });
        var claims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row is not IDictionary<string, object?> values)
            {
                continue;
            }

            if (!TryGetColumn(values, "Claim", out var raw) &&
                !TryGetColumn(values, "ClaimValue", out raw))
            {
                continue;
            }

            var text = raw switch
            {
                null => null,
                string str => str,
                _ => raw.ToString()
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var normalized = text.Trim();

            if (normalized.Length == 0)
            {
                continue;
            }

            claims.Add(normalized);
        }

        if (claims.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[claims.Count];
        claims.CopyTo(result);
        return result;
    }

    private static bool TryGetColumn(IDictionary<string, object?> row, string column, out object? value)
    {
        foreach (var (key, columnValue) in row)
        {
            if (string.Equals(key, column, StringComparison.OrdinalIgnoreCase))
            {
                value = columnValue;
                return true;
            }
        }

        value = null;
        return false;
    }
}
