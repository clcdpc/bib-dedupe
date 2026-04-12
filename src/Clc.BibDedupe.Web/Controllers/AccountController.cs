using System.Security.Claims;
using System.Threading.Tasks;
using Clc.BibDedupe.Web;
using Clc.BibDedupe.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Clc.BibDedupe.Web.Controllers;

[AllowAnonymous]
public class AccountController(IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.TakeAuthMessage();

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> SwitchUser()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.TakeAuthMessage();

        var redirectUri = Url.Action("Index", "Pairs") ?? "/";
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };
        properties.Items["prompt"] = "select_account";

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> Impersonate([FromQuery] string email, [FromQuery] string apiKey)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(apiKey))
        {
            return Unauthorized();
        }

        var configuredApiKey = configuration["Impersonation:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredApiKey)
            || !string.Equals(apiKey, configuredApiKey, System.StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, UserRoles.Administrator)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return RedirectToAction("Index", "Pairs");
    }
}
