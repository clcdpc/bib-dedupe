using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

public class DecisionsController(IDecisionStore store) : Controller
{
    private readonly IDecisionStore _store = store;

    public async Task<IActionResult> Index()
    {
        var user = User?.Identity?.Name ?? string.Empty;
        var items = await _store.GetAllAsync(user);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int leftBibId, int rightBibId, DupeBibPairActions action)
    {
        var user = User?.Identity?.Name ?? string.Empty;
        var decision = new DecisionItem { LeftBibId = leftBibId, RightBibId = rightBibId, Action = action };
        await _store.UpdateAsync(user, decision);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int leftBibId, int rightBibId)
    {
        var user = User?.Identity?.Name ?? string.Empty;
        await _store.RemoveAsync(user, leftBibId, rightBibId);
        return RedirectToAction(nameof(Index));
    }
}
