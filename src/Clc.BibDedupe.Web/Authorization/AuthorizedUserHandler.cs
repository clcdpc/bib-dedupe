using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Authorization;

public class AuthorizedUserHandler : AuthorizationHandler<AuthorizedUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizedUserRequirement requirement)
    {
        if (context.User.IsInRole(UserRoles.Access) || context.User.IsInRole(UserRoles.Administrator))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
