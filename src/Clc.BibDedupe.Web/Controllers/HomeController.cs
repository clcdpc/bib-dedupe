using Clc.BibDedupe.Web.Models;
using Clc.Polaris.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;

namespace Clc.BibDedupe.Web.Controllers
{
    public class HomeController(ILogger<HomeController> logger, IPapiClient papi) : Controller
    {
        public IActionResult Index()
        {
            var bibs = papi.Synch_BibsByIdGet([3628715, 4001657], true);

            ViewBag.LeftBibXML = MarcXmlRenderer.TransformFile(bibs.Data.GetBibsByIDRows.First().BibliographicRecordXML, "marc-to-html.xslt");
            ViewBag.RightBibXML = MarcXmlRenderer.TransformFile(bibs.Data.GetBibsByIDRows.Last().BibliographicRecordXML, "marc-to-html.xslt");

            return View();
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
        public IActionResult Resolve([FromForm] string action)
        {
            if (!Enum.TryParse<DupeBibPairActions>(action, out _))
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
