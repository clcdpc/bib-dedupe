using System.Linq;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class DecisionsController(
    IDecisionStore store,
    IDecisionSubmissionService submissionService,
    IDecisionBatchTracker batchTracker) : Controller
{
    public async Task<IActionResult> Index()
    {
        var email = User.GetEmail();
        var batch = await submissionService.GetCurrentBatchAsync(email);
        var items = batch is null
            ? await store.GetAllAsync(email)
            : Enumerable.Empty<DecisionItem>();

        var model = DecisionIndexViewModel.Create(items, batch);

        return View(model);
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
        var batch = await batchTracker.GetCurrentAsync(email);

        if (batch is not null)
        {
            return Conflict("A submission is already in progress.");
        }

        await store.RemoveAsync(email, leftBibId, rightBibId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit()
    {
        var email = User.GetEmail();
        var result = await submissionService.SubmitAsync(email);

        if (!result.Success)
        {
            var statusCode = result.BatchStatus is not null ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return StatusCode(statusCode, new { error = result.ErrorMessage, startedAt = result.BatchStatus?.StartedAt });
            }

            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok(new { startedAt = result.BatchStatus?.StartedAt });
        }

        return RedirectToAction(nameof(Index));
    }
}
