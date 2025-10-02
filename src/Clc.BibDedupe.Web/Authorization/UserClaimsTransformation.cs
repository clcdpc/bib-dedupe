using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Clc.BibDedupe.Web.Extensions;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Clc.BibDedupe.Web.Authorization;

public class UserClaimsTransformation(IUserAuthorizationService service) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var email = principal.GetEmail();

        if (string.IsNullOrWhiteSpace(email))
        {
            return principal;
        }

        var claims = await service.GetClaimsAsync(email);

        if (claims.Count == 0)
        {
            return principal;
        }

        if (principal.Identity is ClaimsIdentity identity)
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

        return principal;
    }
}
