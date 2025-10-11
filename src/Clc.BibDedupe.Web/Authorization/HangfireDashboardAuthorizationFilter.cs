using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Authorization;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return httpContext.User.IsInRole(UserRoles.Administrator);
    }
}
