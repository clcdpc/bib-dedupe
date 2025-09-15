using System.Security.Claims;

namespace Clc.BibDedupe.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetEmail(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return string.Empty;
        }

        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? string.Empty;
    }
}
