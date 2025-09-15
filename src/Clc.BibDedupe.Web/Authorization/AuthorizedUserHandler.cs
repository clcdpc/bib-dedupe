using Microsoft.AspNetCore.Authorization;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;

namespace Clc.BibDedupe.Web.Authorization;

public class AuthorizedUserHandler(IUserAuthorizationService service)
    : AuthorizationHandler<AuthorizedUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizedUserRequirement requirement)
    {
        var email = context.User.GetEmail();

        if (!string.IsNullOrEmpty(email) && await service.IsAuthorizedAsync(email))
        {
            context.Succeed(requirement);
        }
    }
}
