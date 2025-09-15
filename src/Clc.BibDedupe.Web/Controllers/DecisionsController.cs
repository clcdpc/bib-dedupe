using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class DecisionsController(IDecisionStore store) : Controller
{
    public async Task<IActionResult> Index()
    {
        var email = User.GetEmail();
        var items = await store.GetAllAsync(email);
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
        var email = User.GetEmail();
        await store.RemoveAsync(email, leftBibId, rightBibId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction(nameof(Index));
    }
}
