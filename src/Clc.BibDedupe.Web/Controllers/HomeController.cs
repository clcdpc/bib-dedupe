using Clc.BibDedupe.Web;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Controllers
{
    [Authorize(Policy = "AuthorizedUser")]
    public class HomeController(
        ILogger<HomeController> logger,
        IRecordLoader loader,
        IBibDupePairRepository repository,
        IDecisionStore decisionStore,
        ICurrentPairStore currentPairStore) : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            var authMessage = HttpContext.Session.TakeAuthMessage();
            var message = authMessage?.Message;
            var userName = authMessage?.UserName;

            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(message))
            {
                return RedirectToAction("Index", "Pairs");
            }

            var model = new HomeIndexViewModel
            {
                Message = message,
                CurrentUserName = userName
            };
            return View(model);
        }

        public async Task<IActionResult> Review(int? leftBibId, int? rightBibId, string? returnUrl)
        {
            var userEmail = User.GetEmail();
            BibDupePair? pair;
            if (leftBibId is null || rightBibId is null)
            {
                var decidedPairs = (await decisionStore.GetAllAsync(userEmail))
                    .Select(d => (d.LeftBibId, d.RightBibId))
                    .ToHashSet();

                pair = (await repository.GetAsync())
                    .FirstOrDefault(p => !decidedPairs.Contains((p.LeftBibId, p.RightBibId)));
                if (pair is null)
                {
                    await currentPairStore.ClearAsync(userEmail);
                    return View(new IndexViewModel());
                }
                leftBibId = pair.LeftBibId;
                rightBibId = pair.RightBibId;
            }
            else
            {
                pair = await repository.GetByBibIdsAsync(leftBibId.Value, rightBibId.Value);
            }

            var (left, right) = await loader.LoadAsync(leftBibId.Value, rightBibId.Value);

            var model = new IndexViewModel
            {
                LeftBibId = leftBibId.Value,
                RightBibId = rightBibId.Value,
                LeftBibXml = MarcXmlRenderer.TransformFile(left.BibXml, "marc-to-html.xslt"),
                RightBibXml = MarcXmlRenderer.TransformFile(right.BibXml, "marc-to-html.xslt"),
                LeftItems = left.Items,
                RightItems = right.Items,
                Matches = pair?.Matches
                    .Select(m => new PairMatch
                    {
                        MatchType = m.MatchType,
                        MatchValue = m.MatchValue
                    })
                    .ToList() ?? new List<PairMatch>(),
                LeftHoldCount = pair?.LeftHoldCount ?? 0,
                RightHoldCount = pair?.RightHoldCount ?? 0,
                TotalHoldCount = pair?.TotalHoldCount ?? 0,
                ReturnUrl = returnUrl
            };

            await currentPairStore.SetAsync(userEmail, new CurrentPair
            {
                LeftBibId = model.LeftBibId,
                RightBibId = model.RightBibId
            });

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
            if (!Enum.TryParse(action, out BibDupePairAction parsed))
            {
                return BadRequest();
            }
            var userEmail = User.GetEmail();
            var pair = await repository.GetByBibIdsAsync(leftBibId, rightBibId)
                ?? new BibDupePair
                {
                    LeftBibId = leftBibId,
                    RightBibId = rightBibId
                };

            var decision = new DecisionItem
            {
                LeftBibId = leftBibId,
                RightBibId = rightBibId,
                LeftTitle = pair.LeftTitle,
                LeftAuthor = pair.LeftAuthor,
                RightTitle = pair.RightTitle,
                RightAuthor = pair.RightAuthor,
                TOM = pair.TOM,
                PrimaryMarcTomId = pair.PrimaryMarcTomId,
                Matches = pair.Matches.Select(m => new PairMatch
                {
                    MatchType = m.MatchType,
                    MatchValue = m.MatchValue
                }).ToList(),
                Action = parsed
            };
            try
            {
                await decisionStore.AddAsync(userEmail, decision);
            }
            catch (ConflictingMergeDecisionException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            await currentPairStore.ClearAsync(userEmail);
            return Ok();
        }
    }
}
