using System.Linq;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Authorization;
using Clc.BibDedupe.Web.Services;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class ListUserAuthorizationServiceTests
{
    [TestMethod]
    public async Task Authorization_Normalizes_Whitespace_And_Casing()
    {
        var service = new ListUserAuthorizationService(new[] { " Test@Example.com " });

        var result = await service.IsAuthorizedAsync("test@example.com");

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Authorization_Returns_False_For_An_Unknown_User()
    {
        var service = new ListUserAuthorizationService(new[] { "user@example.com" });

        var result = await service.IsAuthorizedAsync("other@example.com");

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Authorized_Users_Receive_Access_And_Administrator_Roles()
    {
        var service = new ListUserAuthorizationService(new[] { "user@example.com" });

        var claims = await service.GetClaimsAsync("USER@EXAMPLE.COM");

        claims.Should().Contain(new[] { UserRoles.Access, UserRoles.Administrator });
    }

    [TestMethod]
    public async Task Unauthorized_Users_Receive_No_Roles()
    {
        var service = new ListUserAuthorizationService(new[] { "user@example.com" });

        var claims = await service.GetClaimsAsync("other@example.com");

        claims.Should().BeEmpty();
    }
}
