using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace Clc.BibDedupe.Web.Services;

public class TestFileRecordLoader : IRecordLoader
{
    private readonly string leftXml;
    private readonly string rightXml;
    private readonly Random rng = new();

    public TestFileRecordLoader(IWebHostEnvironment env)
    {
        var basePath = env.ContentRootPath;
        leftXml = File.ReadAllText(Path.Combine(basePath, "TestData", "left.xml"));
        rightXml = File.ReadAllText(Path.Combine(basePath, "TestData", "right.xml"));
    }

    public Task<(RecordData Left, RecordData Right)> LoadAsync(int leftBibId, int rightBibId)
    {
        RecordData Make(string xml)
        {
            var count = rng.Next(1, 6);
            var items = new List<Dictionary<string, string>>(count);
            for (var i = 0; i < count; i++)
            {
                items.Add(new Dictionary<string, string>
                {
                    ["AssignedBranch"] = $"Branch {rng.Next(1, 5)}",
                    ["Collection"] = $"Coll {rng.Next(1, 3)}",
                    ["MaterialType"] = rng.Next(0, 2) == 0 ? "Book" : "AV",
                    ["ShelfLocation"] = $"Shelf {rng.Next(1, 20)}",
                    ["CallNumber"] = $"CN{rng.Next(100, 999)}",
                    ["CircStatus"] = "IN",
                    ["Barcode"] = $"BC{rng.Next(100000, 999999)}"
                });
            }
            return new RecordData { BibXml = xml, Items = items };
        }

        return Task.FromResult((Make(leftXml), Make(rightXml)));
    }
}
