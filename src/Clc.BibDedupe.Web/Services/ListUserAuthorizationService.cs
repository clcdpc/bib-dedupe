namespace Clc.BibDedupe.Web.Services;

public class ListUserAuthorizationService(IEnumerable<string> emails) : IUserAuthorizationService
{
    private readonly HashSet<string> _emails = emails
        .Select(e => e.ToLowerInvariant())
        .ToHashSet();

    public Task<bool> IsAuthorizedAsync(string email) =>
        Task.FromResult(_emails.Contains(email.ToLowerInvariant()));
}
