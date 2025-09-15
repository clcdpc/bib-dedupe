using System.Threading.Tasks;
using Clc.BibDedupe.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
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
}
