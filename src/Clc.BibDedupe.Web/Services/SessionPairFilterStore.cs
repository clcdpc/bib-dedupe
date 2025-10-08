using System;
using System.Text.Json;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Services;

public class SessionPairFilterStore(IHttpContextAccessor accessor) : IPairFilterStore
{
    private const string SessionKeyPrefix = "PairFilters:";

    private ISession? Session => accessor.HttpContext?.Session;

    public Task<PairFilterOptions?> GetAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<PairFilterOptions?>(null);
        }

        var session = Session;
        if (session is null)
        {
            return Task.FromResult<PairFilterOptions?>(null);
        }

        var json = session.GetString(GetKey(userId));
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<PairFilterOptions?>(null);
        }

        try
        {
            var filters = JsonSerializer.Deserialize<PairFilterOptions>(json);
            return Task.FromResult(filters?.Normalize());
        }
        catch (JsonException)
        {
            session.Remove(GetKey(userId));
            return Task.FromResult<PairFilterOptions?>(null);
        }
    }

    public Task SetAsync(string userId, PairFilterOptions? filters)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.CompletedTask;
        }

        var session = Session;
        if (session is null)
        {
            return Task.CompletedTask;
        }

        var key = GetKey(userId);

        if (filters is null || filters.Normalize().IsEmpty)
        {
            session.Remove(key);
            return Task.CompletedTask;
        }

        session.SetString(key, JsonSerializer.Serialize(filters));
        return Task.CompletedTask;
    }

    private static string GetKey(string userId) => SessionKeyPrefix + userId;
}
