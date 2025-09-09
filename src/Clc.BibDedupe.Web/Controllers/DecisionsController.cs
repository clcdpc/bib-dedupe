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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int leftBibId, int rightBibId, DupeBibPairActions action)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var decision = new DecisionItem { LeftBibId = leftBibId, RightBibId = rightBibId, Action = action };
        await _store.UpdateAsync(email, decision);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int leftBibId, int rightBibId)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        await _store.RemoveAsync(email, leftBibId, rightBibId);
        return RedirectToAction(nameof(Index));
    }
}
