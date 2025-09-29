using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clc.BibDedupe.Web.Controllers;

[Authorize(Policy = "AuthorizedUser")]
public class PairsController(IBibDupePairRepository repository) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var email = User.GetEmail();
        var (items, total) = await repository.GetPagedAsync(page, pageSize, email);
        var model = new PairsListViewModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        return View(model);
    }
}
