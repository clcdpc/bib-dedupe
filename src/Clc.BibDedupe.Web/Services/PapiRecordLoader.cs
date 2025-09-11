using Clc.BibDedupe.Web.Models;
using Clc.Polaris.Api;
using Clc.Polaris.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace Clc.BibDedupe.Web.Services;

public class PapiRecordLoader(IPapiClient papi) : IRecordLoader
{
    public Task<(RecordData Left, RecordData Right)> LoadAsync(int leftBibId, int rightBibId)
    {
        var bibResponse = papi.Synch_BibsByIdGet(new[] { leftBibId, rightBibId }, true);
        var bibRows = bibResponse.Data.GetBibsByIDRows;
        var leftXml = bibRows.First().BibliographicRecordXML;
        var rightXml = bibRows.Last().BibliographicRecordXML;

        List<Dictionary<string, string>> LoadItems(int bibId)
        {
            var resp = papi.HoldingsGet(bibId);
            var rows = resp.Data?.BibHoldingsGetRows ?? Enumerable.Empty<BibHoldingsGetRow>();
            return rows.Select(r => new Dictionary<string, string>
            {
                ["Location"] = r.LocationName ?? string.Empty,
                ["Collection"] = r.CollectionName ?? string.Empty,
                ["ShelfLocation"] = r.ShelfLocation ?? string.Empty,
                ["CallNumber"] = r.CallNumber ?? string.Empty,
                ["CircStatus"] = r.CircStatus ?? string.Empty,
                ["Barcode"] = r.Barcode ?? string.Empty
            }).ToList();
        }

        var leftItems = LoadItems(leftBibId);
        var rightItems = LoadItems(rightBibId);

        return Task.FromResult((
            new RecordData { BibXml = leftXml, Items = leftItems },
            new RecordData { BibXml = rightXml, Items = rightItems }
        ));
    }
}
