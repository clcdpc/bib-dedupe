using System.Threading.Tasks;
using Clc.BibDedupe.Web.Extensions;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class BatchesController(
    IDecisionBatchResultStore resultStore,
    IDecisionBatchTracker batchTracker) : Controller
{
    public async Task<IActionResult> Index()
    {
        var email = User.GetEmail();
        var summaries = await resultStore.GetSummariesAsync(email);
        var current = await batchTracker.GetCurrentAsync(email);

        var model = new DecisionBatchListViewModel
        {
            Batches = summaries,
            CurrentBatch = current
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var email = User.GetEmail();
        var detail = await resultStore.GetDetailAsync(email, id);

        if (detail is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Batch {detail.Summary.BatchId}";
        return View(detail);
    }
}
