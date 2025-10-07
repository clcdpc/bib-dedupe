using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository) : Controller
{
    private const int DefaultPageSize = 20;

    public async Task<IActionResult> Index(int page = 1, int? tom = null, string? matchType = null, bool? hasHolds = null)
    {
        var email = User.GetEmail();
        var sanitizedTom = tom.HasValue && tom.Value > 0 ? tom : null;
        var sanitizedMatchType = string.IsNullOrWhiteSpace(matchType) ? null : matchType;
        var hasHoldFilter = hasHolds == true ? true : (bool?)null;
        var result = await repository.GetPagedAsync(page, DefaultPageSize, email, sanitizedTom, sanitizedMatchType, hasHoldFilter);
        var model = new PairsListViewModel
        {
            Items = result.Items,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = result.TotalCount,
            TomOptions = result.TomOptions,
            MatchTypeOptions = result.MatchTypeOptions,
            SelectedTomId = sanitizedTom,
            SelectedMatchType = sanitizedMatchType,
            HasHoldsFilter = hasHolds == true
        };
        return View(model);
    }
}
