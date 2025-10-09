using System.Diagnostics;
using Clc.BibDedupe.Web.Extensions;
using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var authMessage = HttpContext.Session.TakeAuthMessage();
        var message = authMessage?.Message;
        var userName = authMessage?.UserName;

        if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(message))
        {
            return RedirectToAction("Index", "Pairs");
        }

        var model = new LandingPageViewModel
        {
            Message = message,
            CurrentUserName = userName
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
