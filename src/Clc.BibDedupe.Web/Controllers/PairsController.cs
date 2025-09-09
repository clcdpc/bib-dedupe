using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Clc.BibDedupe.Web.Controllers;

public class PairsController(IBibDupePairRepository repository) : Controller
{
    private readonly IBibDupePairRepository _repository = repository;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var (items, total) = await _repository.GetPagedAsync(page, pageSize);
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
