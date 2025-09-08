using Microsoft.AspNetCore.Hosting;

namespace Clc.BibDedupe.Web.Services;

public class TestFileRecordXmlLoader : IRecordXmlLoader
{
    private readonly string leftXml;
    private readonly string rightXml;

    public TestFileRecordXmlLoader(IWebHostEnvironment env)
    {
        var basePath = env.ContentRootPath;
        leftXml = File.ReadAllText(Path.Combine(basePath, "TestData", "left.xml"));
        rightXml = File.ReadAllText(Path.Combine(basePath, "TestData", "right.xml"));
    }

    public Task<(string LeftBibXml, string RightBibXml)> LoadAsync(int leftBibId, int rightBibId)
        => Task.FromResult((leftXml, rightXml));
}
