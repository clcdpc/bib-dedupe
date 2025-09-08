using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.Polaris.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Controllers
{
    public class HomeController(ILogger<HomeController> logger, IPapiClient papi, IBibDupePairRepository repository) : Controller
    {
        public IActionResult Index()
        {
            //var bibs = papi.Synch_BibsByIdGet([3628715, 4001657], true);

            var leftBibXml = "<marc:collection xsi:schemaLocation=\"http://www.loc.gov/MARC21/slim http://www.loc.gov/standards/marcxml/schema/MARC21slim.xsd\"";
            var rightBibXml = "<marc:collection xsi:schemaLocation=\"http://www.loc.gov/MARC21/slim http://www.loc.gov/standards/marcxml/schema/MARC21slim.xsd\"";

            var model = new IndexViewModel
            {
                LeftBibId = 3628715,
                RightBibId = 4001657,
                LeftBibXml = MarcXmlRenderer.TransformFile(leftBibXml, "marc-to-html.xslt"),
                RightBibXml = MarcXmlRenderer.TransformFile(rightBibXml, "marc-to-html.xslt")
            };

            /*
            model.LeftBibXml = MarcXmlRenderer.TransformFile(bibs.Data.GetBibsByIDRows.First().BibliographicRecordXML, "marc-to-html.xslt");
            model.RightBibXml = MarcXmlRenderer.TransformFile(bibs.Data.GetBibsByIDRows.Last().BibliographicRecordXML, "marc-to-html.xslt");
            */

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
