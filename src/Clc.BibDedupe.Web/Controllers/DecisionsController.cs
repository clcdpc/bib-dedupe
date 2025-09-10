using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize]
public class DecisionsController(IDecisionStore store) : Controller
{
    private readonly IDecisionStore _store = store;

    public async Task<IActionResult> Index()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var items = await _store.GetAllAsync(email);
        return View(items);
    }

    public IActionResult Review(int leftBibId, int rightBibId)
    {
        var returnUrl = Url.Action(nameof(Index));
        return RedirectToAction("Index", "Home", new { leftBibId, rightBibId, returnUrl });
    }
}
