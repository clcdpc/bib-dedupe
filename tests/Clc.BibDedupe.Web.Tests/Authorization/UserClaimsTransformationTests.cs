using System.Security.Claims;
using Clc.BibDedupe.Web.Authorization;
using Clc.BibDedupe.Web.Services;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Authorization;

[TestClass]
public class UserClaimsTransformationTests
{
    [TestMethod]
    public async Task TransformAsync_Delegates_To_UserRoleClaimsAugmenter()
    {
        var augmenterMock = new Mock<IUserRoleClaimsAugmenter>(MockBehavior.Strict);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        augmenterMock
            .Setup(a => a.AddRoleClaimsAsync(principal))
            .Returns(Task.CompletedTask);

        var transformation = new UserClaimsTransformation(augmenterMock.Object);

        var transformed = await transformation.TransformAsync(principal);

        transformed.Should().BeSameAs(principal);
        augmenterMock.VerifyAll();
    }
}
