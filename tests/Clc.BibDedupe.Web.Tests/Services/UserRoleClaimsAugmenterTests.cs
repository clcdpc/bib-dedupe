using System.Linq;
using System.Security.Claims;
using Clc.BibDedupe.Web.Authorization;
using Clc.BibDedupe.Web.Services;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class UserRoleClaimsAugmenterTests
{
    [TestMethod]
    public async Task AddRoleClaimsAsync_Adds_Roles_From_Authorization_Service()
    {
        var authorizationServiceMock = new Mock<IUserAuthorizationService>(MockBehavior.Strict);
        authorizationServiceMock
            .Setup(s => s.GetClaimsAsync("user@example.com"))
            .ReturnsAsync(new[] { UserRoles.Access, UserRoles.Administrator });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "user@example.com")
        }, authenticationType: "test"));

        var augmenter = new UserRoleClaimsAugmenter(authorizationServiceMock.Object);

        await augmenter.AddRoleClaimsAsync(principal);

        principal.IsInRole(UserRoles.Access).Should().BeTrue();
        principal.IsInRole(UserRoles.Administrator).Should().BeTrue();
        authorizationServiceMock.VerifyAll();
    }

    [TestMethod]
    public async Task AddRoleClaimsAsync_Does_Not_Add_Duplicates()
    {
        var authorizationServiceMock = new Mock<IUserAuthorizationService>(MockBehavior.Strict);
        authorizationServiceMock
            .Setup(s => s.GetClaimsAsync("user@example.com"))
            .ReturnsAsync(new[] { UserRoles.Access, UserRoles.Access });

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, UserRoles.Access)
        }, authenticationType: "test");

        var principal = new ClaimsPrincipal(identity);
        var augmenter = new UserRoleClaimsAugmenter(authorizationServiceMock.Object);

        await augmenter.AddRoleClaimsAsync(principal);

        principal.Claims.Count(c => c.Type == ClaimTypes.Role && c.Value == UserRoles.Access).Should().Be(1);
        authorizationServiceMock.VerifyAll();
    }
}
