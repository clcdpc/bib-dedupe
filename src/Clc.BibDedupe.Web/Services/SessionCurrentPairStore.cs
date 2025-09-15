using System.Text.Json;
using Clc.BibDedupe.Web.Models;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Services;

public class SessionCurrentPairStore(IHttpContextAccessor accessor) : ICurrentPairStore
{
    private const string SessionKey = "CurrentPair";
    private ISession Session => accessor.HttpContext!.Session;

    private static string GetKey(string userId) =>
        string.IsNullOrEmpty(userId) ? SessionKey : $"{SessionKey}:{userId}";

    public Task<CurrentPair?> GetAsync(string userId)
    {
        var key = GetKey(userId);
        var json = Session.GetString(key);
        if (string.IsNullOrEmpty(json))
        {
            return Task.FromResult<CurrentPair?>(null);
        }

        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<CurrentPair>(json));
        }
        catch (JsonException)
        {
            Session.Remove(key);
            return Task.FromResult<CurrentPair?>(null);
        }
    }

    public Task SetAsync(string userId, CurrentPair currentPair)
    {
        var key = GetKey(userId);
        Session.SetString(key, JsonSerializer.Serialize(currentPair));
        return Task.CompletedTask;
    }

    public Task ClearAsync(string userId)
    {
        var key = GetKey(userId);
        Session.Remove(key);
        return Task.CompletedTask;
    }
}
