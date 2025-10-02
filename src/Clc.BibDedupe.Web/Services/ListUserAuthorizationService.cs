using System;
using System.Collections.Generic;
using System.Linq;
using Clc.BibDedupe.Web.Authorization;

namespace Clc.BibDedupe.Web.Services;

public class ListUserAuthorizationService : IUserAuthorizationService
{
    private readonly HashSet<string> _emails;

    public ListUserAuthorizationService(IEnumerable<string> emails)
    {
        _emails = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(NormalizeEmail)
            .ToHashSet();
    }

    public Task<bool> IsAuthorizedAsync(string email) =>
        Task.FromResult(_emails.Contains(NormalizeEmail(email)));

    public Task<IReadOnlyCollection<string>> GetClaimsAsync(string email)
    {
        if (!_emails.Contains(NormalizeEmail(email)))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        IReadOnlyCollection<string> claims = new[]
        {
            UserRoles.Access,
            UserRoles.Administrator
        };

        return Task.FromResult(claims);
    }

    private static string NormalizeEmail(string email) => email?.Trim().ToLowerInvariant() ?? string.Empty;
}
