using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Tests.TestUtilities;

public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id { get; } = Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var bytes))
        {
            value = bytes;
            return true;
        }

        value = null!;
        return false;
    }

    public string? GetString(string key)
    {
        if (!TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(value);
    }

    public void SetString(string key, string value) =>
        Set(key, Encoding.UTF8.GetBytes(value));
}
