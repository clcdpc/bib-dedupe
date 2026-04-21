using System.Security.Claims;

namespace Clc.BibDedupe.Web.Services;

public interface IUserRoleClaimsAugmenter
{
    Task AddRoleClaimsAsync(ClaimsPrincipal principal);
}
