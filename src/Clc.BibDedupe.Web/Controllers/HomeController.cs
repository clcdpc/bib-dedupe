using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Controllers
{
    public class HomeController(ILogger<HomeController> logger, IRecordXmlLoader loader, IBibDupePairRepository repository) : Controller
    {
        public async Task<IActionResult> Index()
        {
            const int leftBibId = 3628715;
            const int rightBibId = 4001657;

            var (leftBibXml, rightBibXml) = await loader.LoadAsync(leftBibId, rightBibId);

            var model = new IndexViewModel
            {
                LeftBibId = leftBibId,
                RightBibId = rightBibId,
                LeftBibXml = MarcXmlRenderer.TransformFile(leftBibXml, "marc-to-html.xslt"),
                RightBibXml = MarcXmlRenderer.TransformFile(rightBibXml, "marc-to-html.xslt")
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve([FromForm] string action, [FromForm] int leftBibId, [FromForm] int rightBibId)
        {
            if (!Enum.TryParse(action, out DupeBibPairActions parsed))
            {
                return BadRequest();
            }

            var userEmail = User?.Identity?.Name ?? string.Empty;

            switch (parsed)
            {
                case DupeBibPairActions.KeepLeft:
                    await repository.MergeAsync(leftBibId, rightBibId, userEmail);
                    break;
                case DupeBibPairActions.KeepRight:
                    await repository.MergeAsync(rightBibId, leftBibId, userEmail);
                    break;
                case DupeBibPairActions.KeepBoth:
                    await repository.KeepBothAsync(leftBibId, rightBibId, userEmail);
                    break;
                case DupeBibPairActions.Skip:
                    await repository.SkipAsync(leftBibId, rightBibId, userEmail);
                    break;
                default:
                    return BadRequest();
            }

            return Ok();
        }
    }
}
