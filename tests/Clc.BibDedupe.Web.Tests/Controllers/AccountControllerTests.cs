using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clc.BibDedupe.Web.Tests.Controllers;

[TestClass]
public class AccountControllerTests
{
    [TestMethod]
    public async Task Impersonate_Returns_Unauthorized_When_Query_Parameters_Are_Missing()
    {
        var controller = BuildController("expected-key");

        var result = await controller.Impersonate(string.Empty, string.Empty);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task Impersonate_Returns_Unauthorized_When_Api_Key_Does_Not_Match()
    {
        var controller = BuildController("expected-key");

        var result = await controller.Impersonate("user@example.com", "wrong-key");

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task Impersonate_Signs_In_And_Redirects_To_Pairs()
    {
        var authService = new RecordingAuthenticationService();
        var controller = BuildController("expected-key", authService);

        var result = await controller.Impersonate("user@example.com", "expected-key");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        authService.SignedInPrincipal.Should().NotBeNull();
        authService.SignedInPrincipal!.FindFirstValue(ClaimTypes.Email).Should().Be("user@example.com");
        authService.SignOutWasCalled.Should().BeTrue();
    }

    private static AccountController BuildController(string apiKey, RecordingAuthenticationService? authService = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Impersonation:ApiKey"] = apiKey
            })
            .Build();

        var controller = new AccountController(config);
        var httpContext = new DefaultHttpContext();

        if (authService is not null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService>(authService);
            httpContext.RequestServices = services.BuildServiceProvider();
        }

        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }
        public bool SignOutWasCalled { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            scheme.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignOutWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
