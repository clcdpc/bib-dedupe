namespace Clc.BibDedupe.Web.Services;

public interface IRecordXmlLoader
{
    Task<(string LeftBibXml, string RightBibXml)> LoadAsync(int leftBibId, int rightBibId);
}
