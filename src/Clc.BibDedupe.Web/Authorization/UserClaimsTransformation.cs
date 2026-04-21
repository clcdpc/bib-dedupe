using System.Security.Claims;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Clc.BibDedupe.Web.Authorization;

public class UserClaimsTransformation(IUserRoleClaimsAugmenter userRoleClaimsAugmenter) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        await userRoleClaimsAugmenter.AddRoleClaimsAsync(principal);
        return principal;
    }
}
