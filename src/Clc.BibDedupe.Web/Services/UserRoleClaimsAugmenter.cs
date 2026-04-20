using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Clc.BibDedupe.Web.Extensions;

namespace Clc.BibDedupe.Web.Services;

public class UserRoleClaimsAugmenter(IUserAuthorizationService authorizationService) : IUserRoleClaimsAugmenter
{
    public async Task AddRoleClaimsAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true || principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var email = principal.GetEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var claims = await authorizationService.GetClaimsAsync(email);

        if (claims.Count == 0)
        {
            return;
        }

        AddMissingRoleClaims(identity, claims);
    }

    private static void AddMissingRoleClaims(ClaimsIdentity identity, IReadOnlyCollection<string> claims)
    {
        var existingRoles = new HashSet<string>(
            identity.FindAll(identity.RoleClaimType)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim))
            {
                continue;
            }

            var role = claim.Trim();
            if (role.Length == 0)
            {
                continue;
            }

            if (existingRoles.Add(role))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }
        }
    }
}
