using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Clc.BibDedupe.Web.Services;

namespace Clc.BibDedupe.Web.Authorization;

public class AuthorizedUserHandler(IUserAuthorizationService service)
    : AuthorizationHandler<AuthorizedUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizedUserRequirement requirement)
    {
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst("preferred_username")?.Value;

        if (!string.IsNullOrEmpty(email) && await service.IsAuthorizedAsync(email))
        {
            context.Succeed(requirement);
        }
    }
}
