using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Controllers
{
    [Authorize]
    public class HomeController(ILogger<HomeController> logger, IRecordXmlLoader loader, IBibDupePairRepository repository, IDecisionStore decisionStore) : Controller
    {
        public async Task<IActionResult> Index(int? leftBibId, int? rightBibId, string? returnUrl)
        {
            if (leftBibId is null || rightBibId is null)
            {
                var first = (await repository.GetAsync()).FirstOrDefault();
                if (first is null) return View(new IndexViewModel());
                leftBibId = first.LeftBibId;
                rightBibId = first.RightBibId;
            }

            var (leftBibXml, rightBibXml) = await loader.LoadAsync(leftBibId.Value, rightBibId.Value);

            var model = new IndexViewModel
            {
                LeftBibId = leftBibId.Value,
                RightBibId = rightBibId.Value,
                LeftBibXml = MarcXmlRenderer.TransformFile(leftBibXml, "marc-to-html.xslt"),
                RightBibXml = MarcXmlRenderer.TransformFile(rightBibXml, "marc-to-html.xslt"),
                ReturnUrl = returnUrl
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
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
            var decision = new DecisionItem { LeftBibId = leftBibId, RightBibId = rightBibId, Action = parsed };
            await decisionStore.AddAsync(userEmail, decision);
            return Ok();
        }
    }
}
