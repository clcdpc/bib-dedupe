using System;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Extensions;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository, IPairFilterStore pairFilterStore) : Controller
{
    private const int DefaultPageSize = 20;

    public async Task<IActionResult> Index(int page = 1, int? tom = null, string? matchType = null, string? hasHolds = null)
    {
        var email = User.GetEmail();
        var sanitizedTom = tom.HasValue && tom.Value > 0 ? tom : null;
        var sanitizedMatchType = string.IsNullOrWhiteSpace(matchType) ? null : matchType.Trim();
        var sanitizedHasHolds = hasHolds switch
        {
            null or "" => (bool?)null,
            var value when value.Equals("true", StringComparison.OrdinalIgnoreCase) => true,
            var value when value.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
            _ => (bool?)null
        };
        await pairFilterStore.SetAsync(email, new PairFilterOptions
        {
            TomId = sanitizedTom,
            MatchType = sanitizedMatchType,
            HasHolds = sanitizedHasHolds
        });
        var result = await repository.GetPagedAsync(page, DefaultPageSize, email, sanitizedTom, sanitizedMatchType, sanitizedHasHolds);
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
            SelectedHasHolds = sanitizedHasHolds
        };
        return View(model);
    }
}
