using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Services;

public interface IUserAuthorizationService
{
    Task<bool> IsAuthorizedAsync(string email);

    Task<IReadOnlyCollection<string>> GetClaimsAsync(string email);
}
