using System.Security.Claims;
using Clc.BibDedupe.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Tests.Authorization;

[TestClass]
public class AuthorizedUserHandlerTests
{
    [TestMethod]
    public async Task Access_Role_Satisfies_AuthorizedUser_Requirement()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, UserRoles.Access)
        }, "test");

        var user = new ClaimsPrincipal(identity);
        var requirement = new AuthorizedUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);
        var handler = new AuthorizedUserHandler();

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
