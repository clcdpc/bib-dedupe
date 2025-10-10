using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Clc.BibDedupe.Web.Models;

public class PairMatch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string MatchType { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;

    public static List<PairMatch> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<PairMatch>();
        }

        return JsonSerializer.Deserialize<List<PairMatch>>(json, JsonOptions) ?? new List<PairMatch>();
    }

    public static List<PairMatch> CloneList(IEnumerable<PairMatch>? matches)
    {
        if (matches is null)
        {
            return new List<PairMatch>();
        }

        return matches
            .Select(m => new PairMatch
            {
                MatchType = m.MatchType,
                MatchValue = m.MatchValue
            })
            .ToList();
    }
}
