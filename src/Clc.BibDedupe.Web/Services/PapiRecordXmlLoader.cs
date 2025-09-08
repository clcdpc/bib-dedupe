using Clc.Polaris.Api;
using System.Linq;

namespace Clc.BibDedupe.Web.Services;

public class PapiRecordXmlLoader(IPapiClient papi) : IRecordXmlLoader
{
    public Task<(string LeftBibXml, string RightBibXml)> LoadAsync(int leftBibId, int rightBibId)
    {
        var response = papi.Synch_BibsByIdGet(new[] { leftBibId, rightBibId }, true);
        var rows = response.Data.GetBibsByIDRows;
        var leftXml = rows.First().BibliographicRecordXML;
        var rightXml = rows.Last().BibliographicRecordXML;
        return Task.FromResult((leftXml, rightXml));
    }
}
