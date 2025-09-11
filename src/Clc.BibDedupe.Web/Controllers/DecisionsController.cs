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
        return RedirectToAction("Review", "Home", new { leftBibId, rightBibId, returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int leftBibId, int rightBibId)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("preferred_username")?.Value ?? string.Empty;
        await _store.RemoveAsync(email, leftBibId, rightBibId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction(nameof(Index));
    }
}
